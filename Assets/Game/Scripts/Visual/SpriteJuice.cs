using UnityEngine;

namespace MiceToBeHome
{
    /// <summary>
    /// Procedural "juice" for a billboard sprite (Paper Mario style): an optional continuous idle
    /// hop and a one-shot squash-and-stretch "bump". Only touches localPosition + localScale, so it
    /// never fights Billboard (which owns rotation + sorting). The sprite pivot is at the bottom, so
    /// squashing Y keeps the base planted. Uses scaled time so it respects hit-stop / pause.
    /// </summary>
    public class SpriteJuice : MonoBehaviour
    {
        [SerializeField] private bool idleHop = false;
        [SerializeField] private float hopHeight = 0.07f;
        [SerializeField] private float hopSpeed = 5f;
        [SerializeField] private float hopSquash = 0.05f;
        [SerializeField] private float bumpDuration = 0.35f;
        [SerializeField] private float bumpAmount = 0.22f;

        private Vector3 basePos;
        private Vector3 baseScale;
        private bool captured;
        private float phaseOffset;
        private float bumpTimer;
        private float bumpStrength;

        private void Awake()
        {
            Capture();
            phaseOffset = Random.value * 10f;
        }

        private void Capture()
        {
            if (captured)
            {
                return;
            }
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            captured = true;
        }

        public void EnableIdleHop(bool on)
        {
            idleHop = on;
        }

        /// <summary>Play a squash-and-stretch bump. Strength (~0.25..1.4) scales the amount.</summary>
        public void Bump(float strength = 1f)
        {
            Capture();
            bumpStrength = Mathf.Clamp(strength, 0.25f, 1.4f);
            bumpTimer = bumpDuration;
        }

        private void LateUpdate()
        {
            Vector3 pos = basePos;
            float sx = 1f;
            float sy = 1f;

            if (idleHop && hopHeight > 0f)
            {
                float phase = Time.time * hopSpeed + phaseOffset;
                float hop = Mathf.Abs(Mathf.Sin(phase)); // 0 at landing, 1 at peak
                pos.y += hop * hopHeight;

                float land = 1f - hop; // squash when it lands
                sx *= 1f + hopSquash * land;
                sy *= 1f - hopSquash * land;
            }

            if (bumpTimer > 0f)
            {
                bumpTimer -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(bumpTimer / bumpDuration); // 0..1 progress
                float s = bumpAmount * bumpStrength * Mathf.Cos(k * Mathf.PI * 3f) * (1f - k);
                sy *= 1f - s;       // compress vertically on impact, then spring
                sx *= 1f + s * 0.6f; // widen a little
            }

            transform.localPosition = pos;
            transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
    }
}
