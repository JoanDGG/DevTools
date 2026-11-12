using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PixelPlay.Foundation.Unity.UI
{
    /// <summary>Accepts draggable UI items through the Unity EventSystem.</summary>
    public sealed class UIDropTarget : MonoBehaviour, IDropHandler
    {
        /// <summary>Unity event invoked with an item accepted by this target.</summary>
        [Serializable]
        public sealed class ItemDroppedEvent : UnityEvent<UIDraggableItem> { }

        [SerializeField, Tooltip("Leave empty to accept every item type.")] private string acceptedItemType;
        [SerializeField, Tooltip("Moves accepted items to this target's center and makes them children of this object.")] private bool snapAndParentItem = true;
        [SerializeField, Tooltip("Invoked when an item is accepted by this target.")] private ItemDroppedEvent onItemDropped = new ItemDroppedEvent();

        /// <summary>Gets whether accepted items should become children of this target at its center.</summary>
        public bool SnapAndParentItem => snapAndParentItem;

        /// <summary>Gets the inspector callback invoked when an item is accepted.</summary>
        public ItemDroppedEvent OnItemDropped => onItemDropped;

        /// <summary>Changes the accepted item type; an empty value accepts every type.</summary>
        public void SetAcceptedItemType(string value) => acceptedItemType = value ?? string.Empty;

        /// <summary>Sets whether accepted items snap to and become children of this target.</summary>
        public void SetSnapAndParentItem(bool value) => snapAndParentItem = value;

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
        /// <summary>Raises the accepted-item event. Usually called by <see cref="UIDraggableItem"/>.</summary>
        public void NotifyAccepted(UIDraggableItem item) => onItemDropped?.Invoke(item);
    }
}
