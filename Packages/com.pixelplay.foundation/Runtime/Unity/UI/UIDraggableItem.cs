using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PixelPlay.Foundation.Unity.UI
{
    /// <summary>Moves a UI element with the EventSystem and restores it when a drop is rejected.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Serializable]
        public sealed class DropAcceptedEvent : UnityEvent<UIDropTarget> { }

        [SerializeField] private string itemType;
        [SerializeField] private DropAcceptedEvent onDropAccepted = new DropAcceptedEvent();

        private RectTransform _rectTransform;
        private Canvas _dragCanvas;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        private CanvasGroup _canvasGroup;
        private bool _originalBlocksRaycasts;
        private bool _dragging;
        private bool _acceptedDrop;

        /// <summary>Gets the logical type used by drop targets to filter accepted items.</summary>
        public string ItemType => itemType;

        /// <summary>Gets the inspector callback invoked after a drop target accepts this item.</summary>
        public DropAcceptedEvent OnDropAccepted => onDropAccepted;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragCanvas = GetComponentInParent<Canvas>();
            if (_dragCanvas == null)
            {
                Debug.LogWarning("UIDraggableItem needs to be below a Canvas.", this);
                return;
            }

            _dragCanvas = _dragCanvas.rootCanvas;
            _originalParent = transform.parent;
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            _originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetParent(_dragCanvas.transform, true);
            transform.SetAsLastSibling();
            _acceptedDrop = false;
            _dragging = true;

            if (_canvasGroup != null)
            {
                _originalBlocksRaycasts = _canvasGroup.blocksRaycasts;
                _canvasGroup.blocksRaycasts = false;
            }

            UpdateDragPosition(eventData);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging)
                UpdateDragPosition(eventData);
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            FinishDrag();
        }

        /// <summary>Called by a drop target after it accepts this item.</summary>
        public bool TryAcceptDrop(UIDropTarget target)
        {
            if (!_dragging || target == null || !target.Accepts(this))
                return false;

            _acceptedDrop = true;
            if (target.SnapAndParentItem)
            {
                transform.SetParent(target.transform, false);
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            target.NotifyAccepted(this);
            onDropAccepted?.Invoke(target);
            return true;
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            RectTransform canvasRect = _dragCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out Vector3 worldPosition))
            {
                _rectTransform.position = worldPosition;
            }
        }

        private void RestoreOriginalPosition()
        {
            transform.SetParent(_originalParent, false);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalAnchoredPosition;
        }

        private void OnDisable()
        {
            FinishDrag();
        }

        private void FinishDrag()
        {
            if (!_dragging)
                return;

            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = _originalBlocksRaycasts;

            if (!_acceptedDrop)
                RestoreOriginalPosition();

            _dragging = false;
            _dragCanvas = null;
        }
    }
}
