#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Dessentials.Common.Utility
{
    /// <summary>
    /// Pins this GameObject's transform to one of the edge midpoints (or the center) of a target RectTransform.
    /// <para>
    /// In edit mode the object is snapped every editor tick, so it cannot be dragged away from the anchor.
    /// In play mode the position is only refreshed on enable and whenever <see cref="ForceUpdatePosition"/> is
    /// called - there is deliberately no runtime Update.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Utility/Rect Transform Position Anchor")]
    public class RectTransformPositionAnchor : MonoBehaviour
    {
        public enum AnchorDirection
        {
            Middle = 0,
            Left = 1,
            Right = 2,
            Top = 3,
            Bottom = 4,
        }

        [Tooltip("The RectTransform to anchor to.")]
        [SerializeField]
        private RectTransform target;

        [Tooltip("Which point of the target rect to snap to. Middle is the rect center, the others are the midpoint of the matching edge.")]
        [SerializeField]
        private AnchorDirection direction = AnchorDirection.Middle;

        [Tooltip("Extra world space offset applied after the anchor point is resolved.")]
        [SerializeField]
        private Vector3 offset;

        [Tooltip("Optional. Leave empty when the target lives in world space (world space canvas or plain rect). " +
                 "Set it when anchoring a world object to a screen space canvas: the anchor point is projected " +
                 "through this camera, keeping the object's current distance from it.")]
        [SerializeField]
        private Camera anchorCamera;

        private readonly Vector3[] m_corners = new Vector3[4];

        public RectTransform Target => target;

        public AnchorDirection Direction => direction;

        private void OnEnable()
        {
            ForceUpdatePosition();
        }

        /// <summary>
        /// Recomputes the anchor point and snaps this transform onto it.
        /// Call this after the target rect has moved, resized or been re-laid-out.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ForceUpdatePosition()
        {
            if (target == null)
            {
                return;
            }

            transform.position = GetAnchorWorldPosition();
        }

        /// <summary>
        /// Assigns a new target (and optionally a new direction) and snaps to it immediately.
        /// </summary>
        public void SetTarget(RectTransform newTarget, AnchorDirection? newDirection = null)
        {
            target = newTarget;

            if (newDirection.HasValue)
            {
                direction = newDirection.Value;
            }

            ForceUpdatePosition();
        }

        /// <summary>
        /// The world position this transform would be snapped to right now.
        /// </summary>
        public Vector3 GetAnchorWorldPosition()
        {
            if (target == null)
            {
                return transform.position;
            }

            // 0 = bottom left, 1 = top left, 2 = top right, 3 = bottom right.
            target.GetWorldCorners(m_corners);

            Vector3 point;
            switch (direction)
            {
                case AnchorDirection.Left:
                    point = (m_corners[0] + m_corners[1]) * 0.5f;
                    break;
                case AnchorDirection.Right:
                    point = (m_corners[2] + m_corners[3]) * 0.5f;
                    break;
                case AnchorDirection.Top:
                    point = (m_corners[1] + m_corners[2]) * 0.5f;
                    break;
                case AnchorDirection.Bottom:
                    point = (m_corners[0] + m_corners[3]) * 0.5f;
                    break;
                default:
                    point = (m_corners[0] + m_corners[2]) * 0.5f;
                    break;
            }

            if (anchorCamera != null)
            {
                point = ProjectThroughCamera(point);
            }

            return point + offset;
        }

        /// <summary>
        /// Converts a point living in the target canvas' space into a world position in front of
        /// <see cref="anchorCamera"/>, preserving this object's current distance from that camera.
        /// </summary>
        private Vector3 ProjectThroughCamera(Vector3 pRectPoint)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            Camera uiCamera = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, pRectPoint);
            var depth = anchorCamera.WorldToScreenPoint(transform.position).z;

            return anchorCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        }

#if UNITY_EDITOR
        // Editor only: keeps the object locked onto the anchor while authoring the scene.
        // This never runs in play mode, so there is no per-frame cost in a build.
        private void Update()
        {
            if (Application.isPlaying || target == null)
            {
                return;
            }

            var anchored = GetAnchorWorldPosition();

            // Only write when it actually moved, otherwise the scene is flagged dirty on every editor tick.
            if ((transform.position - anchored).sqrMagnitude > 1e-10f)
            {
                transform.position = anchored;
            }
        }
#endif
    }
}
