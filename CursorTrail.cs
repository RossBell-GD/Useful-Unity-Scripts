/*
 * CURSOR TRAIL SETUP
 * ------------------
 * 1. Create or select a Canvas in your Unity scene.
 *
 * 2. Create an empty UI object inside the Canvas and name it
 *    something such as "Cursor Trail".
 *
 * 3. Attach this CursorTrail script to the empty UI object.
 *
 * 4. Create a small UI Image that will act as one trail pixel.
 *    Adjust its:
 *      - Size
 *      - Sprite
 *      - Colour
 *      - Raycast Target setting
 *
 *    Disable "Raycast Target" on the Image so the trail does not
 *    block buttons or other UI interactions.
 *
 * 5. Drag the Image into the Project window to create a prefab.
 *    You can then delete the original Image from the scene.
 *
 * 6. In the CursorTrail component:
 *      - Assign the UI object to Trail Parent.
 *      - Assign the Image prefab to Pixel Prefab.
 *      - Assign the scene Canvas to Canvas.
 *
 * 7. Adjust the trail and animation settings in the Inspector.
 *
 * This script creates a trail of small UI images behind the mouse.
 * It uses object pooling so that trail pixels are reused instead of
 * repeatedly being created and destroyed.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CursorTrail : MonoBehaviour
{
    [Header("References")]

    [Tooltip("The UI RectTransform that will contain all trail pixels.")]
    [SerializeField] private RectTransform trailParent;

    [Tooltip("The UI Image prefab used for each trail pixel.")]
    [SerializeField] private RectTransform pixelPrefab;

    [Tooltip("The Canvas used to convert the mouse position into a UI position.")]
    [SerializeField] private Canvas canvas;


    [Header("Trail Settings")]

    [Tooltip("The minimum amount of time between trail pixel spawns.")]
    [SerializeField] private float spawnInterval = 0.025f;

    [Tooltip("How far the mouse must move before another pixel can spawn.")]
    [SerializeField] private float minimumMoveDistance = 4f;

    [Tooltip("The number of trail pixels created and stored in the object pool.")]
    [SerializeField] private int poolSize = 30;


    [Header("Pixel Animation")]

    [Tooltip("How long each trail pixel remains visible.")]
    [SerializeField] private float lifetime = 0.2f;

    [Tooltip("The scale of a trail pixel when it first appears.")]
    [SerializeField] private float startingScale = 1f;

    [Tooltip("The scale of a trail pixel at the end of its animation.")]
    [SerializeField] private float endingScale = 0f;

    [Tooltip("Adds a small random offset to each pixel's spawn position.")]
    [SerializeField] private float randomPositionOffset = 2f;


    // Stores inactive trail pixels that are ready to be reused.
    private readonly Queue<RectTransform> availablePixels = new();

    // The mouse position used during the previous trail spawn.
    private Vector2 previousMousePosition;

    // Controls how frequently trail pixels can spawn.
    private float spawnTimer;


    private void Awake()
    {
        // Attempt to find a Canvas on this GameObject if one
        // was not manually assigned in the Inspector.
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        // Use this object's RectTransform as the trail parent
        // if no other parent was assigned.
        if (trailParent == null)
        {
            trailParent = GetComponent<RectTransform>();
        }

        // Create all trail pixels when the scene begins.
        CreatePool();

        // Store the initial mouse position so the script can
        // measure how far the cursor has moved.
        previousMousePosition = Input.mousePosition;
    }


    private void Update()
    {
        // Unscaled delta time allows the cursor trail to continue
        // animating even when Time.timeScale is set to zero.
        spawnTimer -= Time.unscaledDeltaTime;

        Vector2 currentMousePosition = Input.mousePosition;

        // Calculate how far the mouse has moved since the
        // previous trail pixel was spawned.
        float moveDistance = Vector2.Distance(
            currentMousePosition,
            previousMousePosition
        );

        // Spawn a pixel when enough time has passed and
        // the mouse has moved far enough.
        if (spawnTimer <= 0f && moveDistance >= minimumMoveDistance)
        {
            SpawnPixel(currentMousePosition);

            // Reset the spawn timer.
            spawnTimer = spawnInterval;

            // Store this position for the next distance check.
            previousMousePosition = currentMousePosition;
        }
    }


    /// <summary>
    /// Creates the trail pixels at the beginning of the scene.
    /// The pixels are disabled and placed into a queue until needed.
    /// </summary>
    private void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            // Create a copy of the pixel prefab under the trail parent.
            RectTransform pixel = Instantiate(
                pixelPrefab,
                trailParent
            );

            // Keep the pixel hidden until it is requested.
            pixel.gameObject.SetActive(false);

            // Add the pixel to the pool of available objects.
            availablePixels.Enqueue(pixel);
        }
    }


    /// <summary>
    /// Takes an available pixel from the pool and places it
    /// at the supplied mouse screen position.
    /// </summary>
    private void SpawnPixel(Vector2 screenPosition)
    {
        // Do not spawn anything if every pooled pixel is currently active.
        if (availablePixels.Count == 0)
        {
            return;
        }

        // Remove the next available pixel from the queue.
        RectTransform pixel = availablePixels.Dequeue();

        // Convert the mouse's screen position into a local position
        // inside the trail parent's RectTransform.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            trailParent,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out Vector2 localPosition
        );

        // Add a small random offset to make the trail feel less uniform.
        localPosition += Random.insideUnitCircle * randomPositionOffset;

        // Position and reset the pixel before displaying it.
        pixel.anchoredPosition = localPosition;
        pixel.localScale = Vector3.one * startingScale;
        pixel.gameObject.SetActive(true);

        // Begin fading and shrinking the pixel.
        StartCoroutine(AnimatePixel(pixel));
    }


    /// <summary>
    /// Shrinks and fades a trail pixel before returning it
    /// to the object pool.
    /// </summary>
    private IEnumerator AnimatePixel(RectTransform pixel)
    {
        // Get the Image component used to control the pixel's colour.
        Image image = pixel.GetComponent<Image>();

        // Store the original colour so it can be restored later.
        Color startingColour = image.color;

        // Create a transparent version of the original colour.
        Color endingColour = startingColour;
        endingColour.a = 0f;

        float elapsed = 0f;

        // Animate until the pixel has reached the end of its lifetime.
        while (elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;

            // Convert elapsed time into a value between zero and one.
            float progress = Mathf.Clamp01(elapsed / lifetime);

            // Gradually change the pixel's scale.
            pixel.localScale = Vector3.one * Mathf.Lerp(
                startingScale,
                endingScale,
                progress
            );

            // Gradually fade the pixel to transparent.
            image.color = Color.Lerp(
                startingColour,
                endingColour,
                progress
            );

            yield return null;
        }

        // Restore the original colour before reusing the pixel.
        image.color = startingColour;

        // Hide the pixel and return it to the pool.
        pixel.gameObject.SetActive(false);
        availablePixels.Enqueue(pixel);
    }
}