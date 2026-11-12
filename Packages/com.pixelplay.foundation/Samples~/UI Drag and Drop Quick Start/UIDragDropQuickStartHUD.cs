using PixelPlay.Foundation.Unity.UI;
using TMPro;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Displays accepted UI drops for the drag and drop quick start.</summary>
    public sealed class UIDragDropQuickStartHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("Drop target whose accepted-item event updates the status label.")] private UIDropTarget dropTarget;
        [SerializeField, Tooltip("TextMeshPro label showing the result of the last accepted drop.")] private TextMeshProUGUI statusLabel;

        private void OnEnable()
        {
            if (dropTarget != null)
                dropTarget.OnItemDropped.AddListener(HandleItemDropped);
        }

        private void OnDisable()
        {
            if (dropTarget != null)
                dropTarget.OnItemDropped.RemoveListener(HandleItemDropped);
        }

        private void HandleItemDropped(UIDraggableItem item)
        {
            if (statusLabel != null)
                statusLabel.text = $"Accepted: {item.ItemType}";
        }
    }
}
