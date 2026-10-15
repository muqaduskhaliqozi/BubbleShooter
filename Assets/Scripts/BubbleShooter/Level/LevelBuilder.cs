using System;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

namespace BubbleShooter.Level
{
    /// <summary>
    /// Chhota, saaf level banata hai. Har bubble grid.AddBubble() se add hoti hai,
    /// isliye size aur position hamesha grid ke mutabiq hoti hai (overlap nahi).
    ///
    /// Setup:
    ///  1. Empty GameObject banao, is par LevelBuilder lagao.
    ///  2. Grid  -> BubbleGrid object, Bubble Prefab -> aapka bubble prefab (BubbleController wala).
    ///  3. Purana level spawner disable karo, warna bubbles double spawn hongi.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BubbleGrid grid;
        [SerializeField] private BubbleController bubblePrefab;

        [Header("Level 1 Shape")]
        [Tooltip("Kitni rows bhari hongi (top se)")]
        [SerializeField] private int rowsToFill = 4;
        [Tooltip("0 = poori row bhari. 5 = sirf beech ke 5 columns (chhota cluster)")]
        [SerializeField] private int columnsToFill = 0;

        [Header("Colors")]
        [Tooltip("Level mein kitne alag colors hon (Colors list khali ho tab)")]
        [SerializeField] private int colorCount = 3;
        [Tooltip("Optional: apni pasand ke colors yahan chuno. Khali ho to enum ke pehle colorCount colors use hote hain.")]
        [SerializeField] private BubbleColor[] colors;

        [Header("Options")]
        [SerializeField] private bool buildOnStart = true;

        private void Start()
        {
            if (buildOnStart) Build();
        }

        public void Build()
        {
            if (grid == null) grid = FindObjectOfType<BubbleGrid>();

            if (grid == null || bubblePrefab == null)
            {
                Debug.LogError("[LevelBuilder] Grid ya Bubble Prefab assign nahi hai.");
                return;
            }

            List<BubbleColor> palette = GetPalette();
            if (palette.Count == 0)
            {
                Debug.LogError("[LevelBuilder] Koi color nahi mila.");
                return;
            }

            grid.ClearGrid();

            int rows = Mathf.Clamp(rowsToFill, 1, grid.MaxRows);

            for (int r = 0; r < rows; r++)
            {
                int rowCols = grid.GetColumnCountForRow(r);
                int count = columnsToFill > 0 ? Mathf.Min(columnsToFill, rowCols) : rowCols;
                int start = (rowCols - count) / 2;   // beech mein rakho

                for (int c = start; c < start + count; c++)
                {
                    BubbleColor color = PickColor(palette, r, c);

                    BubbleController b = Instantiate(bubblePrefab, grid.transform);
                    b.SetColor(color);
                    grid.AddBubble(r, c, b);   // size + position + attach, sab yahin
                }
            }
        }

        private List<BubbleColor> GetPalette()
        {
            var list = new List<BubbleColor>();

            if (colors != null && colors.Length > 0)
            {
                list.AddRange(colors);
                return list;
            }

            foreach (BubbleColor value in Enum.GetValues(typeof(BubbleColor)))
            {
                string n = value.ToString();
                if (n.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(value);
                if (list.Count >= Mathf.Max(1, colorCount)) break;
            }
            return list;
        }

        /// <summary>
        /// Random color, magar left aur upar ke padosi jaisa nahi (jahan tak mumkin),
        /// taake level shuru se hi khud pop na ho.
        /// </summary>
        private BubbleColor PickColor(List<BubbleColor> palette, int row, int col)
        {
            var avoid = new HashSet<BubbleColor>();

            BubbleController left = grid.GetBubble(row, col - 1);
            if (left != null) avoid.Add(left.Color);

            // upar ke dono padosi (parity ka khayal grid.GetNeighbors rakhta hai)
            foreach (GridCoordinate n in grid.GetNeighbors(new GridCoordinate(row, col)))
            {
                if (n.Row < row)
                {
                    BubbleController up = grid.GetBubble(n);
                    if (up != null) avoid.Add(up.Color);
                }
            }

            var options = new List<BubbleColor>();
            foreach (var c in palette)
                if (!avoid.Contains(c)) options.Add(c);

            if (options.Count == 0) options = palette;
            return options[UnityEngine.Random.Range(0, options.Count)];
        }
    }
}