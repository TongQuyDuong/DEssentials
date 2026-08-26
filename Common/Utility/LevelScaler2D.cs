using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Dessentials.Common.Utility
{
    /// <summary>
    /// Uniformly scales and centers this object so that a set of registered "current bound" transforms
    /// fits inside a "maximum bound" rectangle described by four marker transforms.
    /// <para>
    /// Usage: children call <see cref="RegisterBound"/> to declare the extents that must fit, then
    /// <see cref="RescaleAndReposition"/> is called once to apply the fit. <see cref="ClearBounds"/> resets
    /// the accumulated bound before a new level is built.
    /// </para>
    /// <para>
    /// Registered rects are accumulated into a local space rectangle as they come in, so registration is O(1)
    /// and nothing is retained or re-walked at fit time. The consequence is that a bound is captured where it
    /// was at the moment it registered: register after the content has reached its final layout.
    /// </para>
    /// <para>
    /// The two axes accumulate independently, so a rect can take part on one axis and stay completely out of
    /// the other. If only one axis ever gets a registration, the fit scales by that axis alone and leaves the
    /// other axis' position untouched.
    /// </para>
    /// <para>
    /// The scale is uniform and stops as soon as the two bounds meet on either axis (a best fit, not a stretch),
    /// capped by <see cref="maxScale"/>. If anything about the inputs makes the fit impossible, the transform is
    /// restored to the position and scale it had before the call and the method returns false.
    /// </para>
    /// </summary>
    [AddComponentMenu("Utility/Level Scaler 2D")]
    public class LevelScaler2D : MonoBehaviour
    {
        /// <summary>
        /// Axes of a registered rect that should be left out of the current bound entirely. An ignored axis
        /// contributes nothing at all on that axis, not even the rect's center, so it can neither widen the
        /// bound nor pull its center around. <see cref="Both"/> makes the whole registration a no-op.
        /// </summary>
        [Flags]
        public enum RectAxis
        {
            None = 0,
            Horizontal = 1 << 0,
            Vertical = 1 << 1,
            Both = Horizontal | Vertical,
        }

        private const float EPSILON = 1e-5f;

        [Header("Maximum Bound")]
        [Tooltip("Marker transform for the left edge of the area the content must fit into. Only its X is used.")]
        [SerializeField]
        private Transform leftBound;

        [Tooltip("Marker transform for the right edge of the area the content must fit into. Only its X is used.")]
        [SerializeField]
        private Transform rightBound;

        [Tooltip("Marker transform for the top edge of the area the content must fit into. Only its Y is used.")]
        [SerializeField]
        private Transform topBound;

        [Tooltip("Marker transform for the bottom edge of the area the content must fit into. Only its Y is used.")]
        [SerializeField]
        private Transform bottomBound;

        [Header("Scaling")]
        [Tooltip("Upper limit for the resulting uniform local scale. The content is never blown up past this even if the maximum bound would allow it.")]
        [SerializeField]
        private float maxScale = 1f;

        // The registered bound, accumulated in this object's local space so that rescaling this transform does
        // not invalidate it. The axes are tracked separately because a rect can opt out of one of them, which
        // leaves that axis with no registered span at all rather than with a zero width one.
        private float m_localMinX;
        private float m_localMaxX;
        private float m_localMinY;
        private float m_localMaxY;
        private bool m_hasHorizontal;
        private bool m_hasVertical;
        private int m_boundCount;

#if UNITY_EDITOR
        // The maximum bound as the last fit resolved it. Kept so the gizmo can show the rect the fit
        // actually ran against instead of re-reading the marker transforms, which are anchored to a
        // screen space rect and so sit wherever the anchors last snapped them.
        private Vector2 m_lastMaxBoundMin;
        private Vector2 m_lastMaxBoundMax;
        private bool m_hasLastMaxBound;
#endif

        /// <summary>
        /// How many rects have contributed to the current bound since the last <see cref="ClearBounds"/>.
        /// A rect that ignored both axes contributed nothing and is not counted.
        /// </summary>
        public int RegisteredBoundCount => m_boundCount;

        /// <summary>
        /// Whether anything has been registered on the horizontal axis.
        /// </summary>
        public bool HasHorizontalBound => m_hasHorizontal;

        /// <summary>
        /// Whether anything has been registered on the vertical axis.
        /// </summary>
        public bool HasVerticalBound => m_hasVertical;

        /// <summary>
        /// Expands the current bound to include a rect centered on <paramref name="pBound"/>.
        /// </summary>
        /// <param name="pBound">Transform whose position is the center of the rect.</param>
        /// <param name="pRectSize">
        /// Width and height of the rect, in this object's local space, which is the space the level content is
        /// authored in. While this object sits at scale 1 (the usual state when registering) that is the same as
        /// world units. Pass <see cref="Vector2.zero"/> to register the center point on its own.
        /// </param>
        /// <param name="pIgnoredAxes">
        /// Axes to stay out of completely. The rect contributes neither extent nor center on an ignored axis,
        /// so content that moves along one axis can be kept from influencing the bound there at all.
        /// </param>
        /// <remarks>
        /// The rect is captured immediately, so later movement of <paramref name="pBound"/> is not reflected in
        /// the fit. Registering the same transform twice is harmless.
        /// </remarks>
        public void RegisterBound(Transform pBound, Vector2 pRectSize, RectAxis pIgnoredAxes = RectAxis.None)
        {
            if (pBound == null)
            {
                Debug.LogWarning($"{nameof(LevelScaler2D)}: tried to register a null bound, ignoring.", this);
                return;
            }

            if (pIgnoredAxes == RectAxis.Both) return;

            var center = transform.InverseTransformPoint(pBound.position);

            // A zero scale on this object (or a parent) makes the conversion blow up to infinity or NaN.
            if (!IsFinite(center.x) || !IsFinite(center.y))
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: could not convert '{pBound.name}' to local space, check for a zero scale on this object or a parent.", this);
                return;
            }

            if (!IsFinite(pRectSize.x) || !IsFinite(pRectSize.y))
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: '{pBound.name}' was registered with a non finite rect size ({pRectSize}), ignoring.", this);
                return;
            }

            // Abs so a negative size still describes a rect rather than inverting it.
            if ((pIgnoredAxes & RectAxis.Horizontal) == 0)
            {
                var halfWidth = Mathf.Abs(pRectSize.x) * 0.5f;

                FoldAxis(center.x - halfWidth, center.x + halfWidth, ref m_localMinX, ref m_localMaxX, ref m_hasHorizontal);
            }

            if ((pIgnoredAxes & RectAxis.Vertical) == 0)
            {
                var halfHeight = Mathf.Abs(pRectSize.y) * 0.5f;

                FoldAxis(center.y - halfHeight, center.y + halfHeight, ref m_localMinY, ref m_localMaxY, ref m_hasVertical);
            }

            m_boundCount++;
        }

        /// <summary>
        /// Discards the accumulated current bound. Call this before rebuilding the content.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ClearBounds()
        {
            m_boundCount = 0;
            m_hasHorizontal = false;
            m_hasVertical = false;
            m_localMinX = 0f;
            m_localMaxX = 0f;
            m_localMinY = 0f;
            m_localMaxY = 0f;
        }

        /// <summary>
        /// Uniformly rescales this transform so the registered bound fits the maximum bound, then repositions
        /// it so the registered bound is centered inside the maximum bound. Only axes that something registered
        /// on are scaled against or recentered.
        /// </summary>
        /// <returns>
        /// True when the fit was applied. False when the inputs were invalid, in which case the original
        /// position and scale are restored.
        /// </returns>
#if ODIN_INSPECTOR
        [Button]
#endif
        public bool RescaleAndReposition()
        {
            var originalPosition = transform.position;
            var originalScale = transform.localScale;

            if (!TryFit(originalScale))
            {
                transform.position = originalPosition;
                transform.localScale = originalScale;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Does the actual work. Every failure path leaves it to the caller to restore the transform.
        /// </summary>
        private bool TryFit(Vector3 pOriginalScale)
        {
            if (maxScale <= EPSILON)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: {nameof(maxScale)} must be greater than zero.", this);
                return false;
            }

            // The current scale is the base the fit ratio multiplies, so it cannot be zero or negative.
            var currentScale = pOriginalScale.x;
            if (currentScale <= EPSILON)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: current local scale ({currentScale}) must be positive to be rescaled.", this);
                return false;
            }

            if (!m_hasHorizontal && !m_hasVertical)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: no current bounds registered on either axis, call {nameof(RegisterBound)} first.", this);
                return false;
            }

            if (!TryGetMaxBound(out var maxMin, out var maxMax))
            {
                return false;
            }

            GetCurrentBoundWorld(out var contentMin, out var contentMax);

            var maxSize = maxMax - maxMin;
            var contentSize = contentMax - contentMin;

            // Fit ratio: how much the content has to grow/shrink to touch the maximum bound. An axis takes part
            // only if something registered on it and it ended up with a real extent, so a single row or column
            // of bounds still fits by its other axis instead of resolving to an infinite ratio.
            var ratio = float.PositiveInfinity;

            if (m_hasHorizontal && contentSize.x > EPSILON)
            {
                ratio = Mathf.Min(ratio, maxSize.x / contentSize.x);
            }

            if (m_hasVertical && contentSize.y > EPSILON)
            {
                ratio = Mathf.Min(ratio, maxSize.y / contentSize.y);
            }

            if (float.IsInfinity(ratio))
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: the registered bounds have no extent on any registered axis, nothing to scale against.", this);
                return false;
            }

            var newScale = Mathf.Min(currentScale * ratio, maxScale);

            if (!IsFinite(newScale) || newScale <= EPSILON)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: computed an invalid scale ({newScale}).", this);
                return false;
            }

            transform.localScale = new Vector3(newScale, newScale, newScale);

            // The bound moved with the rescale. Re-projecting the stored local rect is a handful of matrix ops,
            // so measure again rather than predicting where it landed.
            GetCurrentBoundWorld(out contentMin, out contentMax);

            var contentCenter = (contentMin + contentMax) * 0.5f;
            var maxCenter = (maxMin + maxMax) * 0.5f;

            // An axis nothing registered on has no center to align, so it keeps whatever position it had.
            var delta = new Vector3(
                m_hasHorizontal ? maxCenter.x - contentCenter.x : 0f,
                m_hasVertical ? maxCenter.y - contentCenter.y : 0f,
                0f);

            transform.position += delta;
            return true;
        }

        /// <summary>
        /// Folds one axis' span into the accumulated min/max, seeding it on the first contribution.
        /// </summary>
        private static void FoldAxis(float pMin, float pMax, ref float pAccumulatedMin, ref float pAccumulatedMax, ref bool pHasAxis)
        {
            if (!pHasAxis)
            {
                pAccumulatedMin = pMin;
                pAccumulatedMax = pMax;
                pHasAxis = true;
                return;
            }

            pAccumulatedMin = Mathf.Min(pAccumulatedMin, pMin);
            pAccumulatedMax = Mathf.Max(pAccumulatedMax, pMax);
        }

        /// <summary>
        /// Builds the maximum bound rectangle from the four marker transforms.
        /// </summary>
        private bool TryGetMaxBound(out Vector2 pMin, out Vector2 pMax)
        {
            pMin = Vector2.zero;
            pMax = Vector2.zero;

            if (leftBound == null || rightBound == null || topBound == null || bottomBound == null)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: all four maximum bound transforms must be assigned.", this);
                return false;
            }

            pMin = new Vector2(leftBound.position.x, bottomBound.position.y);
            pMax = new Vector2(rightBound.position.x, topBound.position.y);

            var size = pMax - pMin;

            if (size.x <= EPSILON || size.y <= EPSILON)
            {
                Debug.LogError($"{nameof(LevelScaler2D)}: the maximum bound has no area (size {size}). Check that left/right and bottom/top are not swapped or overlapping.", this);
                return false;
            }

#if UNITY_EDITOR
            m_lastMaxBoundMin = pMin;
            m_lastMaxBoundMax = pMax;
            m_hasLastMaxBound = true;
#endif

            return true;
        }

        /// <summary>
        /// Projects the accumulated local bound back into world space. All four corners are transformed so the
        /// result stays a correct world axis aligned box even if this object is rotated or mirrored.
        /// <para>
        /// An axis nothing registered on falls back to this object's own pivot line purely so the projection has
        /// a coordinate to work with. Callers must gate on <see cref="m_hasHorizontal"/> /
        /// <see cref="m_hasVertical"/> and ignore the result on an axis that was never registered.
        /// </para>
        /// </summary>
        private void GetCurrentBoundWorld(out Vector2 pMin, out Vector2 pMax)
        {
            var minX = m_hasHorizontal ? m_localMinX : 0f;
            var maxX = m_hasHorizontal ? m_localMaxX : 0f;
            var minY = m_hasVertical ? m_localMinY : 0f;
            var maxY = m_hasVertical ? m_localMaxY : 0f;

            var a = transform.TransformPoint(minX, minY, 0f);
            var b = transform.TransformPoint(minX, maxY, 0f);
            var c = transform.TransformPoint(maxX, maxY, 0f);
            var d = transform.TransformPoint(maxX, minY, 0f);

            pMin = new Vector2(
                Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)),
                Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y)));

            pMax = new Vector2(
                Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)),
                Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y)));
        }

        private static bool IsFinite(float pValue)
        {
            return !float.IsNaN(pValue) && !float.IsInfinity(pValue);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Red is the maximum bound the content has to fit into, magenta is what has been registered so far.
        /// After a successful fit the magenta rect sits centered inside the red one and touches it on the
        /// axis that drove the scale. The magenta rect only exists once something has registered, so it shows
        /// up in play mode, and it flattens to a line on an axis nothing registered on.
        /// <para>
        /// The red rect is the bound the last fit resolved, not a live read of the marker transforms, so it
        /// keeps showing what the fit was actually measured against. Until the first fit runs there is no
        /// resolved bound, so the markers' current positions stand in for it.
        /// </para>
        /// </summary>
        private void OnDrawGizmos()
        {
            var previousColor = Gizmos.color;

            if (m_hasLastMaxBound)
            {
                Gizmos.color = Color.red;
                DrawRectGizmo(m_lastMaxBoundMin, m_lastMaxBoundMax);
            }
            else if (leftBound != null && rightBound != null && topBound != null && bottomBound != null)
            {
                Gizmos.color = Color.red;
                DrawRectGizmo(
                    new Vector2(leftBound.position.x, bottomBound.position.y),
                    new Vector2(rightBound.position.x, topBound.position.y));
            }

            if (m_hasHorizontal || m_hasVertical)
            {
                GetCurrentBoundWorld(out var contentMin, out var contentMax);

                Gizmos.color = Color.magenta;
                DrawRectGizmo(contentMin, contentMax);
            }

            Gizmos.color = previousColor;
        }

        private void DrawRectGizmo(Vector2 pMin, Vector2 pMax)
        {
            var center = new Vector3((pMin.x + pMax.x) * 0.5f, (pMin.y + pMax.y) * 0.5f, transform.position.z);
            var size = new Vector3(Mathf.Abs(pMax.x - pMin.x), Mathf.Abs(pMax.y - pMin.y), 0f);

            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
