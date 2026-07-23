using UnityEngine;
using UnityEngine.EventSystems;

namespace MiceToBeHome
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView tooltip;
        private string message;

        public void Setup(TooltipView tooltipView, string tooltipMessage)
        {
            tooltip = tooltipView;
            message = tooltipMessage;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Show(message);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }
    }
}
