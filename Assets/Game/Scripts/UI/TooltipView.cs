using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiceToBeHome
{
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform panel = null;
        [SerializeField] private TextMeshProUGUI label = null;
        private RectTransform canvasRect;

        public void Initialize()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            Hide();
        }

        public void Show(string message)
        {
            if (label != null)
            {
                label.text = message;
            }
            gameObject.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (Mouse.current == null || canvasRect == null || panel == null)
            {
                return;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
            {
                panel.anchoredPosition = local + new Vector2(18f, -18f);
            }
        }
    }
}
