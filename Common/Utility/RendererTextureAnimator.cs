// File: MaterialAnimator.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Animates a material's texture property by cycling through a list of textures at a given FPS.
/// Automatically creates a unique material instance to avoid modifying the shared material asset.
/// </summary>
[AddComponentMenu("Animation/Material Animator")]
public class RendererTextureAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The list of textures to cycle through for the animation.")]
    public List<Texture2D> textures;

    [Tooltip("The target frames per second for the animation.")]
    [Range(1, 120)]
    public int fps = 12;

    [Header("Material Settings")]
    [Tooltip("The Renderer component whose material will be animated (e.g., MeshRenderer, SpriteRenderer). If left null, it will be found on this GameObject at startup.")]
    public Renderer targetRenderer;

    [Tooltip("The name of the texture property in the shader to animate (e.g., _MainTex, _BaseMap).")]
    public string materialPropertyName = "_MainTex";

    // --- Private state ---
    private Material _materialInstance; // The unique instance of the material for this object
    private int _currentIndex = 0;
    private float _timer = 0f;

    void Awake()
    {
        // If a target renderer isn't assigned manually, try to get one from this GameObject.
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError("MaterialAnimator: No Renderer component found on this GameObject or assigned in the inspector. Disabling script.", this);
            this.enabled = false;
            return;
        }

        // IMPORTANT: Accessing .material creates a new instance of the material for this renderer.
        // This prevents us from changing the shared material asset for all other objects.
        _materialInstance = targetRenderer.material;

        // Set the initial texture immediately to avoid a single-frame flash of the original texture.
        if (textures != null && textures.Count > 0)
        {
            if (_materialInstance.HasProperty(materialPropertyName))
            {
                _materialInstance.SetTexture(materialPropertyName, textures[0]);
            }
            else
            {
                Debug.LogError($"MaterialAnimator: The material does not have a texture property named '{materialPropertyName}'. Disabling script.", this);
                this.enabled = false;
            }
        }
    }

    void Update()
    {
        // Do nothing if we don't have enough data to animate.
        if (_materialInstance == null || textures == null || textures.Count <= 1 || fps <= 0)
        {
            return;
        }

        // Calculate how long each frame should be displayed.
        float frameDuration = 1.0f / fps;

        // Accumulate time passed since last frame.
        _timer += Time.deltaTime;

        // If enough time has passed to show the next frame...
        if (_timer >= frameDuration)
        {
            // Decrement the timer, carrying over any excess time.
            // This makes the animation more accurate, especially with fluctuating frame rates.
            _timer -= frameDuration;

            // Advance to the next texture index, looping back to the start if we reach the end.
            _currentIndex = (_currentIndex + 1) % textures.Count;

            // Apply the new texture to our unique material instance.
            _materialInstance.SetTexture(materialPropertyName, textures[_currentIndex]);
        }
    }

    /// <summary>
    /// When this GameObject is destroyed, we should clean up the material instance we created
    /// to prevent memory leaks, especially when working in the editor.
    /// </summary>
    void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }
}