using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dessentials.Extensions
{
    public static class DImageExtensions
    {
        /// <summary>
        /// Sets the width of the Image's RectTransform to match the sprite's native aspect ratio,
        /// while preserving the current height of the RectTransform.
        /// Does nothing if the Image or its sprite is null.
        /// </summary>
        /// <param name="image">The Image component to modify.</param>
        public static void SetNativeAspectRatioByHeight(this Image image)
        {
            // --- Safety Checks ---
            if (image == null)
            {
                Debug.LogError("SetNativeAspectRatio called on a null Image component.");
                return;
            }

            Sprite sprite = image.sprite;

            if (sprite == null)
            {
                // If there's no sprite, there's no aspect ratio to match.
                // You could log a warning, but often it's better for UI tools to fail silently.
                // Debug.LogWarning("Cannot set native aspect ratio on an Image with a null sprite.", image.gameObject);
                return;
            }

            RectTransform rectTransform = image.rectTransform;
            float nativeHeight = sprite.rect.height;

            // Avoid division by zero for sprites with no height.
            if (nativeHeight <= 0)
            {
                return;
            }

            // --- Calculation ---
            float nativeWidth = sprite.rect.width;
            float currentHeight = rectTransform.rect.height;

            // The core calculation: newWidth / currentHeight = nativeWidth / nativeHeight
            float aspectRatio = nativeWidth / nativeHeight;
            float newWidth = currentHeight * aspectRatio;

            // --- Applying the New Size ---
            // SetSizeWithCurrentAnchors is a robust way to set the width regardless of anchor settings.
            // It preserves the current height automatically when only setting the horizontal axis.
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }
        
        /// <summary>
        /// Sets the height of the Image's RectTransform to match the sprite's native aspect ratio,
        /// while preserving the current width of the RectTransform.
        /// Does nothing if the Image or its sprite is null.
        /// </summary>
        /// <param name="image">The Image component to modify.</param>
        public static void SetNativeAspectRatioByWidth(this Image image)
        {
            // --- Safety Checks ---
            if (image == null)
            {
                Debug.LogError("SetNativeAspectRatioByWidth called on a null Image component.");
                return;
            }

            Sprite sprite = image.sprite;

            if (sprite == null)
            {
                // Debug.LogWarning("Cannot set native aspect ratio on an Image with a null sprite.", image.gameObject);
                return;
            }
        
            RectTransform rectTransform = image.rectTransform;
            float nativeWidth = sprite.rect.width;

            // Avoid division by zero for sprites with no width.
            if (nativeWidth <= 0)
            {
                return;
            }

            // --- Calculation ---
            float nativeHeight = sprite.rect.height;
            float currentWidth = rectTransform.rect.width;
        
            // The core calculation: currentWidth / newHeight = nativeWidth / nativeHeight
            // Rearranged: newHeight = currentWidth * (nativeHeight / nativeWidth)
            float aspectRatio = nativeHeight / nativeWidth;
            float newHeight = currentWidth * aspectRatio;

            // --- Applying the New Size ---
            // SetSizeWithCurrentAnchors is used to set the height of the vertical axis.
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        }
    }
}
