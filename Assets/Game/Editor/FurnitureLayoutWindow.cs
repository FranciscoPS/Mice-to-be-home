#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public class FurnitureLayoutWindow : EditorWindow
    {
        private GameConfig config;

        [MenuItem("Tools/Mice to be Home/Furniture Layout")]
        public static void Open()
        {
            var window = GetWindow<FurnitureLayoutWindow>("Furniture Layout");
            window.minSize = new Vector2(340f, 360f);
        }

        private void OnEnable()
        {
            config = FindConfig();
        }

        private static GameConfig FindConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("No GameConfig found. Run Tools > Mice to be Home > Build Scene once to create it.", MessageType.Info);
                if (GUILayout.Button("Refresh"))
                {
                    config = FindConfig();
                }
                return;
            }

            BalanceSettings balance = config.Balance;
            int cols = Mathf.Max(1, balance.gridColumns);
            int rows = Mathf.Max(1, balance.gridRows);
            EnsureLayout(balance, cols, rows);

            EditorGUILayout.LabelField($"Grid  {cols} x {rows}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Click a cell to toggle furniture. Top row is the far side.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("'S' marks the mouse spawn - keep it clear.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            int centerCol = cols / 2;
            int centerGridRow = rows / 2;
            bool changed = false;
            bool spawnBlocked = false;

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int c = 0; c < cols; c++)
                {
                    int gridRow = rows - 1 - r;
                    bool isSpawn = c == centerCol && gridRow == centerGridRow;
                    bool furniture = balance.furnitureRows[r][c] == 'X' || balance.furnitureRows[r][c] == 'x';

                    if (isSpawn && furniture)
                    {
                        spawnBlocked = true;
                    }

                    string label = furniture ? "X" : (isSpawn ? "S" : string.Empty);
                    bool toggled = GUILayout.Toggle(furniture, label, "Button", GUILayout.Width(36f), GUILayout.Height(36f));
                    if (toggled != furniture)
                    {
                        SetCell(balance, r, c, toggled);
                        changed = true;
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            if (spawnBlocked)
            {
                EditorGUILayout.HelpBox("The spawn cell has furniture - the mouse would start stuck inside it.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear all"))
            {
                Fill(balance, cols, rows, false);
                changed = true;
            }
            if (GUILayout.Button("Border"))
            {
                BorderPreset(balance, cols, rows);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();

            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Layout is saved to the GameConfig. Rebuild the scene to place the furniture.", MessageType.None);
            if (GUILayout.Button("Build Scene (apply layout)", GUILayout.Height(30f)))
            {
                SceneBuilder.BuildScene();
            }
        }

        private static void EnsureLayout(BalanceSettings balance, int cols, int rows)
        {
            string[] src = balance.furnitureRows ?? new string[0];
            var result = new string[rows];
            for (int r = 0; r < rows; r++)
            {
                string line = r < src.Length ? (src[r] ?? string.Empty) : string.Empty;
                var chars = new char[cols];
                for (int c = 0; c < cols; c++)
                {
                    char existing = c < line.Length ? line[c] : '.';
                    chars[c] = existing == 'X' || existing == 'x' ? 'X' : '.';
                }
                result[r] = new string(chars);
            }
            balance.furnitureRows = result;
        }

        private static void SetCell(BalanceSettings balance, int r, int c, bool furniture)
        {
            char[] chars = balance.furnitureRows[r].ToCharArray();
            chars[c] = furniture ? 'X' : '.';
            balance.furnitureRows[r] = new string(chars);
        }

        private static void Fill(BalanceSettings balance, int cols, int rows, bool furniture)
        {
            for (int r = 0; r < rows; r++)
            {
                balance.furnitureRows[r] = new string(furniture ? 'X' : '.', cols);
            }
        }

        private static void BorderPreset(BalanceSettings balance, int cols, int rows)
        {
            for (int r = 0; r < rows; r++)
            {
                var chars = new char[cols];
                for (int c = 0; c < cols; c++)
                {
                    bool edge = r == 0 || r == rows - 1 || c == 0 || c == cols - 1;
                    chars[c] = edge ? 'X' : '.';
                }
                balance.furnitureRows[r] = new string(chars);
            }
        }
    }
}
#endif
