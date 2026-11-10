using PixelPlay.Foundation.Unity.UI;
using TMPro;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Displays accepted UI drops for the drag and drop quick start.</summary>
    public sealed class UIDragDropQuickStartHUD : MonoBehaviour
    {
        [SerializeField] private UIDropTarget dropTarget;
        [SerializeField] private TextMeshProUGUI statusLabel;

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
