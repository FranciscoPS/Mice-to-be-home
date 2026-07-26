using UnityEngine;

namespace MiceToBeHome
{
    public class FurniturePiece : MonoBehaviour
    {
        private SpriteJuice juice;
        private float lastBump = -1f;

        private void Awake()
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                juice = sr.GetComponent<SpriteJuice>();
                if (juice == null)
                {
                    juice = sr.gameObject.AddComponent<SpriteJuice>();
                }
            }
        }

        /// <summary>Squash-and-stretch when an actor bumps into this piece (rate-limited).</summary>
        public void Bump(float strength)
        {
            if (juice == null || Time.time - lastBump < 0.2f)
            {
                return;
            }
            lastBump = Time.time;
            juice.Bump(strength);
        }
    }
}
