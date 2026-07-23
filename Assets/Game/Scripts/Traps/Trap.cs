using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class Trap : MonoBehaviour
    {
        public TrapDefinition Definition { get; private set; }
        public Orientation Orientation { get; private set; }
        public IReadOnlyList<Vector2Int> Cells => cells;

        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private SpriteRenderer visual;
        private Color baseColor;
        private float cooldown;
        private float cooldownRemaining;
        private bool armed;

        public bool IsAvailable => armed && cooldownRemaining <= 0f;
        public float EffectSeconds => Definition != null ? Definition.effectSeconds : 0f;
        public float DistractionRadius => Definition != null ? Definition.distractionRadius : 0f;

        public void Initialize(TrapDefinition definition, IReadOnlyList<Vector2Int> footprint, Orientation orientation, SpriteRenderer renderer, float cooldownSeconds)
        {
            Definition = definition;
            Orientation = orientation;
            visual = renderer;
            cooldown = cooldownSeconds;
            baseColor = renderer != null ? renderer.color : Color.white;

            cells.Clear();
            for (int i = 0; i < footprint.Count; i++)
            {
                cells.Add(footprint[i]);
            }

            cooldownRemaining = 0f;
            armed = false;
            RefreshVisual();
        }

        public void SetArmed(bool value)
        {
            armed = value;
            cooldownRemaining = 0f;
            RefreshVisual();
        }

        public float Trigger()
        {
            cooldownRemaining = cooldown;
            RefreshVisual();
            return EffectSeconds;
        }

        private void OnEnable() => TrapRegistry.Register(this);

        private void OnDisable() => TrapRegistry.Unregister(this);

        private void Update()
        {
            if (cooldownRemaining <= 0f)
            {
                return;
            }

            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining <= 0f)
            {
                cooldownRemaining = 0f;
                RefreshVisual();
            }
        }

        private void RefreshVisual()
        {
            if (visual == null)
            {
                return;
            }

            bool dimmed = armed && cooldownRemaining > 0f;
            Color color = baseColor;
            color.a = dimmed ? 0.35f : 1f;
            visual.color = color;
        }
    }
}
