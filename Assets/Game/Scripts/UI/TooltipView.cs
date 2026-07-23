using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiceToBeHome
{
    public class TooltipView : MonoBehaviour
    {
        private RectTransform panel;
        private TextMeshProUGUI label;
        private RectTransform canvasRect;

        public void Build(Canvas canvas)
        {
            canvasRect = canvas.GetComponent<RectTransform>();

            var image = UIFactory.CreatePanel(transform, "TooltipPanel", new Color(0.05f, 0.05f, 0.08f, 0.92f));
            panel = image.rectTransform;
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(360f, 150f));

            label = UIFactory.CreateText(panel, "Text", string.Empty, 24f, TextAlignmentOptions.TopLeft, Color.white);
            UIFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(14f, 14f);
            label.rectTransform.offsetMax = new Vector2(-14f, -14f);

            Hide();
        }

        public void Show(string message)
        {
            label.text = message;
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
            if (Mouse.current == null || canvasRect == null)
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
