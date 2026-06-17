using System.Collections.Generic;
#if DOTWEEN
using DG.Tweening;
#endif
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Dessentials.Utility
{
    public enum GridFillDirection
    {
        Right,
        Left,
        Down,
        Up
    }

    public enum GridAlignment
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum GridConstraint
    {
        FixedColumnCount,
        FixedRowCount
    }

    public class GameObjectGridLayout2D : MonoBehaviour
    {
        [SerializeField] private GridFillDirection _fillDirection = GridFillDirection.Down;
        [SerializeField] private GridAlignment _alignment = GridAlignment.MiddleCenter;
        [SerializeField] private GridConstraint _constraint = GridConstraint.FixedColumnCount;
        [SerializeField] private int _constraintCount = 3;
        [SerializeField] private Vector2 _spacing = Vector2.one;
        [SerializeField] private Vector2 _offset;
        [SerializeField] private bool _centerIncomplete;

        public void RepositionNow(Vector2 spacing, Vector2 offset)
        {
            _spacing = spacing;
            _offset = offset;
            RepositionNow();
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void RepositionNow()
        {
            var children = GetActiveChildren();
            var count = children.Count;
            if (count == 0) return;

            ComputeGridSize(count, out int cols, out int rows);
            var offset = ComputeAlignmentOffset(cols, rows);

            for (int i = 0; i < count; i++)
            {
                ComputeCellPosition(i, cols, rows, out int col, out int row);
                var incomplete = ComputeIncompleteOffset(i, count, cols, rows);

                children[i].localPosition = new Vector3(
                    _offset.x + offset.x + incomplete.x + col * _spacing.x,
                    _offset.y + offset.y + incomplete.y - row * _spacing.y,
                    0f
                );
            }
        }

#if DOTWEEN
#if ODIN_INSPECTOR
        [Button]
#endif
        public Sequence RepositionSmooth(float duration = 0.2f)
        {
            var children = GetActiveChildren();
            var count = children.Count;
            var seq = DOTween.Sequence();
            if (count == 0) return seq;

            ComputeGridSize(count, out int cols, out int rows);
            var offset = ComputeAlignmentOffset(cols, rows);

            for (int i = 0; i < count; i++)
            {
                ComputeCellPosition(i, cols, rows, out int col, out int row);
                var incomplete = ComputeIncompleteOffset(i, count, cols, rows);

                var target = new Vector3(
                    _offset.x + offset.x + incomplete.x + col * _spacing.x,
                    _offset.y + offset.y + incomplete.y - row * _spacing.y,
                    0f
                );

                var child = children[i];
                child.DOKill();
                seq.Join(child.DOLocalMove(target, duration));
            }

            return seq;
        }
#endif

        public Vector3 GetPositionAtIndex(int index)
        {
            var count = Mathf.Max(index + 1, GetActiveChildCount());
            ComputeGridSize(count, out int cols, out int rows);
            var offset = ComputeAlignmentOffset(cols, rows);
            var incomplete = ComputeIncompleteOffset(index, count, cols, rows);

            ComputeCellPosition(index, cols, rows, out int col, out int row);

            return new Vector3(
                offset.x + incomplete.x + col * _spacing.x,
                offset.y + incomplete.y - row * _spacing.y,
                0f
            );
        }

        private void ComputeGridSize(int count, out int cols, out int rows)
        {
            var safeConstraint = Mathf.Max(1, _constraintCount);

            if (_constraint == GridConstraint.FixedColumnCount)
            {
                cols = safeConstraint;
                rows = (count + cols - 1) / cols;
            }
            else
            {
                rows = safeConstraint;
                cols = (count + rows - 1) / rows;
            }
        }

        private void ComputeCellPosition(int index, int cols, int rows, out int col, out int row)
        {
            switch (_fillDirection)
            {
                case GridFillDirection.Down:
                    col = index % cols;
                    row = index / cols;
                    break;
                case GridFillDirection.Up:
                    col = index % cols;
                    row = (rows - 1) - index / cols;
                    break;
                case GridFillDirection.Right:
                    row = index % rows;
                    col = index / rows;
                    break;
                case GridFillDirection.Left:
                    row = index % rows;
                    col = (cols - 1) - index / rows;
                    break;
                default:
                    col = index % cols;
                    row = index / cols;
                    break;
            }
        }

        private Vector2 ComputeIncompleteOffset(int index, int count, int cols, int rows)
        {
            if (!_centerIncomplete) return Vector2.zero;

            bool vertical = _fillDirection == GridFillDirection.Down
                         || _fillDirection == GridFillDirection.Up;

            if (vertical)
            {
                int row = index / cols;
                int rowStart = row * cols;
                int itemsInRow = Mathf.Min(cols, count - rowStart);
                if (itemsInRow >= cols) return Vector2.zero;

                float shift = (cols - itemsInRow) * _spacing.x * 0.5f;
                return new Vector2(shift, 0f);
            }
            else
            {
                int col = index / rows;
                int colStart = col * rows;
                int itemsInCol = Mathf.Min(rows, count - colStart);
                if (itemsInCol >= rows) return Vector2.zero;

                float shift = (rows - itemsInCol) * _spacing.y * 0.5f;
                return new Vector2(0f, -shift);
            }
        }

        private Vector2 ComputeAlignmentOffset(int cols, int rows)
        {
            float gridWidth = (cols - 1) * _spacing.x;
            float gridHeight = (rows - 1) * _spacing.y;

            float offsetX;
            switch (_alignment)
            {
                case GridAlignment.TopLeft:
                case GridAlignment.MiddleLeft:
                case GridAlignment.BottomLeft:
                    offsetX = 0f;
                    break;
                case GridAlignment.TopRight:
                case GridAlignment.MiddleRight:
                case GridAlignment.BottomRight:
                    offsetX = -gridWidth;
                    break;
                default:
                    offsetX = -gridWidth * 0.5f;
                    break;
            }

            float offsetY;
            switch (_alignment)
            {
                case GridAlignment.TopLeft:
                case GridAlignment.TopCenter:
                case GridAlignment.TopRight:
                    offsetY = 0f;
                    break;
                case GridAlignment.BottomLeft:
                case GridAlignment.BottomCenter:
                case GridAlignment.BottomRight:
                    offsetY = gridHeight;
                    break;
                default:
                    offsetY = gridHeight * 0.5f;
                    break;
            }

            return new Vector2(offsetX, offsetY);
        }

        private List<Transform> GetActiveChildren()
        {
            var list = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf)
                    list.Add(child);
            }
            return list;
        }

        private int GetActiveChildCount()
        {
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                    count++;
            }
            return count;
        }
    }
}
