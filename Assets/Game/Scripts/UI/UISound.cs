using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiceToBeHome
{
    /// <summary>Plays UI hover/click SFX. Added to every button by UIFactory.CreateButton.</summary>
    public class UISound : MonoBehaviour, IPointerEnterHandler
    {
        private AudioManager cached;

        private AudioManager Audio
        {
            get
            {
                if (cached == null)
                {
                    cached = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
                }
                return cached;
            }
        }

        private void Start()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(PlayClick);
            }
        }

        private void PlayClick()
        {
            if (Audio != null)
            {
                Audio.PlayClick();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Audio != null)
            {
                Audio.PlayHover();
            }
        }
    }
}
