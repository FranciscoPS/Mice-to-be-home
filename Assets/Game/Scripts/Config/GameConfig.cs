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
        [SerializeField] private List<Trap> trapPrefabs = new List<Trap>();

        public SpriteLibrary Sprites => sprites;
        public BalanceSettings Balance => balance;
        public AudioBank Audio => audio;
        public IReadOnlyList<Trap> Traps => trapPrefabs;

        public void EnsureDefaults()
        {
            sprites ??= new SpriteLibrary();
            balance ??= new BalanceSettings();
            audio ??= new AudioBank();
            trapPrefabs ??= new List<Trap>();
        }

        public static List<TrapDefinition> BuildDefaultTraps()
        {
            return new List<TrapDefinition>
            {
                MakeTrap("Toy Mouse", "A quick, cheap decoy.", 1f, 3, new Color(0.60f, 0.63f, 0.65f)),
                MakeTrap("Yarn Ball", "Rolls around and grabs the cat's attention.", 1.25f, 3, new Color(0.91f, 0.42f, 0.57f)),
                MakeTrap("Toy Bird", "Chirps and hops to steal its attention.", 1.5f, 2, new Color(0.95f, 0.76f, 0.31f)),
                MakeTrap("Feather Wand", "Dances in the air and hypnotizes it.", 1.75f, 2, new Color(0.69f, 0.52f, 0.80f)),
                MakeTrap("Cardboard Box", "Irresistible - the cat just has to climb in.", 2f, 2, new Color(0.76f, 0.57f, 0.35f)),
                MakeTrap("Scratching Post", "It loves sharpening its claws here.", 2.25f, 1, new Color(0.85f, 0.70f, 0.35f)),
                MakeTrap("Tuna Can", "The smell keeps it busy for a good while.", 2.5f, 1, new Color(0.50f, 0.70f, 0.84f)),
                MakeTrap("Catnip", "Leaves it totally dazed - your panic button.", 3f, 1, new Color(0.50f, 0.69f, 0.41f))
            };
        }

        private static TrapDefinition MakeTrap(string name, string description, float seconds, int stock, Color tint)
        {
            return new TrapDefinition
            {
                displayName = name,
                description = description,
                effectSeconds = seconds,
                stock = stock,
                tint = tint
            };
        }

#if UNITY_EDITOR
        public void EditorSetTrapPrefabs(List<Trap> prefabs)
        {
            trapPrefabs = prefabs;
        }
#endif
    }
}
