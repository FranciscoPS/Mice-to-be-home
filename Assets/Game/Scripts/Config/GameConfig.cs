using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Mice to be Home/Game Config")]
    public class GameConfig : ScriptableObject
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
                MakeTrap("Yarn Ball", "Rolls around and grabs the cat's attention.", 3f, 1, new Color(0.91f, 0.42f, 0.57f)),
                MakeTrap("Toy Mouse", "A quick, cheap decoy.", 2f, 1, new Color(0.60f, 0.63f, 0.65f)),
                MakeTrap("Cardboard Box", "Irresistible - the cat just has to climb in.", 3f, 2, new Color(0.76f, 0.57f, 0.35f)),
                MakeTrap("Tuna Can", "The smell keeps it busy for a good while.", 5f, 1, new Color(0.50f, 0.70f, 0.84f)),
                MakeTrap("Scratching Post", "It loves sharpening its claws here.", 4f, 2, new Color(0.85f, 0.70f, 0.35f)),
                MakeTrap("Catnip", "Leaves it dazed for several seconds.", 5f, 1, new Color(0.50f, 0.69f, 0.41f)),
                MakeTrap("Feather Wand", "Dances in the air and hypnotizes it.", 3f, 2, new Color(0.69f, 0.52f, 0.80f)),
                MakeTrap("Toy Bird", "Chirps and hops to steal its attention.", 3f, 1, new Color(0.95f, 0.76f, 0.31f))
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
