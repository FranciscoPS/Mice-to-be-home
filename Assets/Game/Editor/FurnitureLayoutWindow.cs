#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    public class FurnitureLayoutWindow : EditorWindow
    {
        private GameConfig config;
        private char brush = FurnitureCatalog.Types[0].Code;

        [MenuItem("Tools/Mice to be Home/Furniture Layout")]
        public static void Open()
        {
            var window = GetWindow<FurnitureLayoutWindow>("Furniture Layout");
            window.minSize = new Vector2(440f, 480f);
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
            bool changed = EnsureLayout(balance, cols, rows);

            EditorGUILayout.LabelField($"Grid  {cols} x {rows}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Pick a type, then click cells to paint. Top row is the far side.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("'S' = mouse spawn (keep it clear). A 1x2 also fills the cell to its right ('>').", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            DrawPalette();
            EditorGUILayout.Space();
            changed |= DrawGrid(balance, cols, rows);

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear all"))
            {
                Fill(balance, cols, rows);
                changed = true;
            }

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

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawBrushButton(FurnitureCatalog.Empty, "Erase", Color.gray);
            for (int i = 0; i < FurnitureCatalog.Types.Length; i++)
            {
                FurnitureType type = FurnitureCatalog.Types[i];
                DrawBrushButton(type.Code, type.Width >= 2 ? type.Name + " 1x2" : type.Name, type.Tint);
                if ((i + 1) % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrushButton(char code, string label, Color tint)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = brush == code ? Color.white : tint;
            if (GUILayout.Button(label, GUILayout.Height(24f)))
            {
                brush = code;
            }
            GUI.backgroundColor = prev;
        }

        private bool DrawGrid(BalanceSettings balance, int cols, int rows)
        {
            int centerCol = cols / 2;
            int centerGridRow = rows / 2;
            bool changed = false;
            bool spawnBlocked = false;
            Color prev = GUI.backgroundColor;

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int c = 0; c < cols; c++)
                {
                    int gridRow = rows - 1 - r;
                    bool isSpawn = c == centerCol && gridRow == centerGridRow;
                    char ch = balance.furnitureRows[r][c];

                    string label;
                    Color color;
                    if (ch == FurnitureCatalog.Continuation)
                    {
                        label = ">";
                        color = new Color(0.55f, 0.55f, 0.55f);
                    }
                    else if (FurnitureCatalog.TryGet(ch, out FurnitureType type))
                    {
                        label = type.Code.ToString();
                        color = type.Tint;
                    }
                    else
                    {
                        label = isSpawn ? "S" : string.Empty;
                        color = isSpawn ? new Color(0.3f, 0.7f, 0.95f) : new Color(0.85f, 0.85f, 0.85f);
                    }

                    if (isSpawn && ch != FurnitureCatalog.Empty)
                    {
                        spawnBlocked = true;
                    }

                    GUI.backgroundColor = color;
                    if (GUILayout.Button(label, GUILayout.Width(34f), GUILayout.Height(34f)))
                    {
                        PaintCell(balance, r, c, cols);
                        changed = true;
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = prev;

            if (spawnBlocked)
            {
                EditorGUILayout.HelpBox("The spawn cell has furniture - the mouse would start stuck inside it.", MessageType.Warning);
            }
            return changed;
        }

        private void PaintCell(BalanceSettings balance, int r, int c, int cols)
        {
            ClearPairAt(balance, r, c, cols);

            if (brush == FurnitureCatalog.Empty)
            {
                return;
            }

            if (FurnitureCatalog.IsDouble(brush))
            {
                if (c + 1 >= cols)
                {
                    return; // a 1x2 needs a free cell to its right
                }
                ClearPairAt(balance, r, c + 1, cols);
                SetChar(balance, r, c, brush);
                SetChar(balance, r, c + 1, FurnitureCatalog.Continuation);
            }
            else
            {
                SetChar(balance, r, c, brush);
            }
        }

        // Remove whatever occupies (r,c), keeping 1x2 anchor/continuation pairs consistent.
        private static void ClearPairAt(BalanceSettings balance, int r, int c, int cols)
        {
            char ch = balance.furnitureRows[r][c];
            if (ch == FurnitureCatalog.Continuation)
            {
                if (c - 1 >= 0)
                {
                    SetChar(balance, r, c - 1, FurnitureCatalog.Empty);
                }
                SetChar(balance, r, c, FurnitureCatalog.Empty);
            }
            else if (FurnitureCatalog.IsDouble(ch))
            {
                SetChar(balance, r, c, FurnitureCatalog.Empty);
                if (c + 1 < cols)
                {
                    SetChar(balance, r, c + 1, FurnitureCatalog.Empty);
                }
            }
            else if (ch != FurnitureCatalog.Empty)
            {
                SetChar(balance, r, c, FurnitureCatalog.Empty);
            }
        }

        private static void SetChar(BalanceSettings balance, int r, int c, char value)
        {
            char[] chars = balance.furnitureRows[r].ToCharArray();
            chars[c] = value;
            balance.furnitureRows[r] = new string(chars);
        }

        private static void Fill(BalanceSettings balance, int cols, int rows)
        {
            for (int r = 0; r < rows; r++)
            {
                balance.furnitureRows[r] = new string(FurnitureCatalog.Empty, cols);
            }
        }

        // Resize to the grid and sanitize: map legacy 'X'/'x', drop unknown chars, and keep 1x2
        // anchor/continuation pairs consistent. Returns true if anything changed.
        private static bool EnsureLayout(BalanceSettings balance, int cols, int rows)
        {
            string[] src = balance.furnitureRows ?? new string[0];
            var result = new string[rows];
            for (int r = 0; r < rows; r++)
            {
                string line = r < src.Length ? (src[r] ?? string.Empty) : string.Empty;
                var chars = new char[cols];
                for (int c = 0; c < cols; c++)
                {
                    char existing = FurnitureCatalog.Normalize(c < line.Length ? line[c] : FurnitureCatalog.Empty);
                    chars[c] = existing == FurnitureCatalog.Continuation || FurnitureCatalog.TryGet(existing, out _)
                        ? existing
                        : FurnitureCatalog.Empty;
                }
                result[r] = new string(chars);
            }

            for (int r = 0; r < rows; r++)
            {
                var chars = result[r].ToCharArray();
                for (int c = 0; c < cols; c++)
                {
                    if (FurnitureCatalog.IsDouble(chars[c]))
                    {
                        if (c + 1 < cols && chars[c + 1] == FurnitureCatalog.Empty)
                        {
                            chars[c + 1] = FurnitureCatalog.Continuation;
                        }
                        else if (c + 1 >= cols || chars[c + 1] != FurnitureCatalog.Continuation)
                        {
                            chars[c] = FurnitureCatalog.Empty; // 1x2 that no longer fits
                        }
                    }
                    else if (chars[c] == FurnitureCatalog.Continuation &&
                             (c - 1 < 0 || !FurnitureCatalog.IsDouble(chars[c - 1])))
                    {
                        chars[c] = FurnitureCatalog.Empty; // orphan continuation
                    }
                }
                result[r] = new string(chars);
            }

            bool changed = balance.furnitureRows == null || balance.furnitureRows.Length != rows;
            for (int r = 0; !changed && r < rows; r++)
            {
                if (balance.furnitureRows[r] != result[r])
                {
                    changed = true;
                }
            }
            balance.furnitureRows = result;
            return changed;
        }
    }
}
#endif
