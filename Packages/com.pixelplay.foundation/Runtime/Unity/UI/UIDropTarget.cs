using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PixelPlay.Foundation.Unity.UI
{
    /// <summary>Accepts draggable UI items through the Unity EventSystem.</summary>
    public sealed class UIDropTarget : MonoBehaviour, IDropHandler
    {
        [Serializable]
        public sealed class ItemDroppedEvent : UnityEvent<UIDraggableItem> { }

        [SerializeField, Tooltip("Leave empty to accept every item type.")] private string acceptedItemType;
        [SerializeField] private bool snapAndParentItem = true;
        [SerializeField] private ItemDroppedEvent onItemDropped = new ItemDroppedEvent();

        /// <summary>Gets whether accepted items should become children of this target at its center.</summary>
        public bool SnapAndParentItem => snapAndParentItem;

        /// <summary>Gets the inspector callback invoked when an item is accepted.</summary>
        public ItemDroppedEvent OnItemDropped => onItemDropped;

        /// <inheritdoc />
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            UIDraggableItem item = eventData.pointerDrag.GetComponent<UIDraggableItem>();
            if (item != null)
                item.TryAcceptDrop(this);
        }

        /// <summary>Checks whether this target accepts the supplied logical item type.</summary>
        public bool Accepts(UIDraggableItem item)
        {
            return item != null && (string.IsNullOrWhiteSpace(acceptedItemType) ||
                                    string.Equals(acceptedItemType, item.ItemType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Invokes the target's inspector callback after an accepted drop.</summary>
        public void NotifyAccepted(UIDraggableItem item) => onItemDropped?.Invoke(item);
    }
}
