using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class GameConfig
    {
        [SerializeField] private SpriteLibrary sprites = new SpriteLibrary();
        [SerializeField] private BalanceSettings balance = new BalanceSettings();
        [SerializeField] private AudioBank audio = new AudioBank();
        [SerializeField] private List<TrapDefinition> traps = new List<TrapDefinition>();

        public SpriteLibrary Sprites => sprites;
        public BalanceSettings Balance => balance;
        public AudioBank Audio => audio;
        public IReadOnlyList<TrapDefinition> Traps => traps;

        public void EnsureDefaults()
        {
            sprites ??= new SpriteLibrary();
            balance ??= new BalanceSettings();
            audio ??= new AudioBank();

            if (traps == null || traps.Count == 0)
            {
                traps = BuildDefaultTraps();
            }
        }

        public static List<TrapDefinition> BuildDefaultTraps()
        {
            return new List<TrapDefinition>
            {
                MakeTrap("Bola de estambre", "Rueda y llama la atencion del gato.", 3f, 1, new Color(0.91f, 0.42f, 0.57f)),
                MakeTrap("Raton de juguete", "Un senuelo rapido y economico.", 2f, 1, new Color(0.60f, 0.63f, 0.65f)),
                MakeTrap("Caja de carton", "Irresistible: el gato tiene que meterse.", 3f, 2, new Color(0.76f, 0.57f, 0.35f)),
                MakeTrap("Lata de atun", "El olor lo mantiene ocupado un buen rato.", 5f, 1, new Color(0.50f, 0.70f, 0.84f)),
                MakeTrap("Rascador", "Le encanta afilar sus garras aqui.", 4f, 2, new Color(0.85f, 0.70f, 0.35f)),
                MakeTrap("Catnip", "Lo deja en las nubes por varios segundos.", 5f, 1, new Color(0.50f, 0.69f, 0.41f)),
                MakeTrap("Liston con plumas", "Baila con el aire y lo hipnotiza.", 3f, 2, new Color(0.69f, 0.52f, 0.80f)),
                MakeTrap("Pajaro de juguete", "Pia y salta para robar su atencion.", 3f, 1, new Color(0.95f, 0.76f, 0.31f))
            };
        }

        private static TrapDefinition MakeTrap(string name, string description, float seconds, int size, Color tint)
        {
            return new TrapDefinition
            {
                displayName = name,
                description = description,
                effectSeconds = seconds,
                gridSize = size,
                distractionRadius = size >= 2 ? 2f : 1.6f,
                tint = tint
            };
        }
    }
}
