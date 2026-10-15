using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;

namespace BubbleShooter.Grid
{
    public class BubbleGrid : MonoBehaviour
    {
        [Header("Grid Config")]
        [SerializeField] private int maxRows = 14;
        [SerializeField] private int maxColumns = 8;
        [SerializeField] private float bubbleDiameter = 0.8f;
        [SerializeField] private Vector2 gridOrigin = new Vector2(-2.8f, 3.5f);

        [Header("Layout")]
        [Tooltip("OFF: sab rows seedhi ek doosre ke neeche (saaf square grid). " +
                 "ON: zigzag, har dusri row aadhi bubble khisaki hui (classic bubble shooter).")]
        [SerializeField] private bool staggerRows = false;

        private BubbleController[,] grid;
        private float rowHeight;

        // Har baar ShiftRowsDown par toggle hota hai, taake bubbles seedhi neeche jayein
        private int rowParityShift = 0;

        // ---- Horizontal row movement (moving targets) ----
        private float[] rowOffsets;          // har row ka abhi ka x-offset (world units)
        private float[] rowDirections;       // har row ki direction: +1 (dayen) ya -1 (bayen)
        private bool rowMotionEnabled;
        private float rowMotionSpeed;
        private bool alternateRowDirections = true;
        private bool rowMotionPaused;

        public int MaxRows => maxRows;
        public int MaxColumns => maxColumns;
        public float BubbleDiameter => bubbleDiameter;
        public Vector2 GridOrigin => gridOrigin;
        public bool StaggerRows => staggerRows;

        /// <summary>Kya is level mein rows horizontally hil rahi hain?</summary>
        public bool IsRowMotionEnabled => rowMotionEnabled;
        // Alternate OFF ho to sab rows yeh ek shared offset/direction use karti hain
        private float sharedOffset = 0f;
        private float sharedDirection = 1f;

        /// <summary>true karne par rows ki motion ruk jati hai (jaise nayi row ki animation ke dauran).</summary>
        public bool RowMotionPaused
        {
            get => rowMotionPaused;
            set => rowMotionPaused = value;
        }

        private void Awake()
        {
            InitializeGrid(maxRows, maxColumns);
        }

        // ---------------------------------------------------------------
        // Row parity helpers
        // ---------------------------------------------------------------

        private bool IsOddRow(int row)
        {
            // Square layout mein koi row aadhi bubble khisaki hui nahi hoti
            if (!staggerRows) return false;

            return ((row + rowParityShift) % 2) != 0;
        }

        /// <summary>Row ke valid columns ki tadad (odd row mein 1 kam).</summary>
        public int GetColumnCountForRow(int row)
        {
            return IsOddRow(row) ? maxColumns - 1 : maxColumns;
        }

        public bool IsOddRowPublic(int row)
        {
            return IsOddRow(row);
        }

        // ---------------------------------------------------------------
        // Init
        // ---------------------------------------------------------------

        public void InitializeGrid(int rows, int cols)
        {
            maxRows = Mathf.Max(1, rows);
            maxColumns = Mathf.Max(2, cols);

            grid = new BubbleController[maxRows, maxColumns];

            rowOffsets = new float[maxRows];
            rowDirections = new float[maxRows];
            ResetRowMotionState();

            // Zigzag: rows thodi paas (sqrt(3)/2). Square: rows bilkul ek diameter door.
            rowHeight = staggerRows ? bubbleDiameter * 0.8660254f : bubbleDiameter;
            rowParityShift = 0;

            // Center the full-width rows horizontally.
            gridOrigin.x = -((maxColumns - 1) * bubbleDiameter) / 2f;

            // Keep the existing vertical starting position.
            gridOrigin.y = 3.6f;
        }

        // ---------------------------------------------------------------
        // Position conversion
        // ---------------------------------------------------------------

        public Vector3 GridToWorldPosition(int row, int col)
        {
            float xOffset = IsOddRow(row) ? (bubbleDiameter * 0.5f) : 0f;
            float x = gridOrigin.x + (col * bubbleDiameter) + xOffset + GetRowMotionOffset(row);
            float y = gridOrigin.y - (row * rowHeight);
            return new Vector3(x, y, 0f);
        }

        public Vector3 GridToWorldPosition(GridCoordinate coord)
        {
            return GridToWorldPosition(coord.Row, coord.Column);
        }

        public GridCoordinate WorldToGridPosition(Vector3 worldPos)
        {
            float approxRow = (gridOrigin.y - worldPos.y) / rowHeight;
            int r = Mathf.Clamp(Mathf.RoundToInt(approxRow), 0, maxRows - 1);

            float xOffset = IsOddRow(r) ? (bubbleDiameter * 0.5f) : 0f;
            float approxCol = (worldPos.x - gridOrigin.x - xOffset - GetRowMotionOffset(r)) / bubbleDiameter;
            int c = Mathf.Clamp(Mathf.RoundToInt(approxCol), 0, GetColumnCountForRow(r) - 1);

            GridCoordinate bestCoord = new GridCoordinate(r, c);
            float minDistance = Vector3.Distance(worldPos, GridToWorldPosition(r, c));

            // Check neighbors for closest cell snap
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = r + dr;
                    int nc = c + dc;
                    if (IsValidCoordinate(nr, nc))
                    {
                        float dist = Vector3.Distance(worldPos, GridToWorldPosition(nr, nc));
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestCoord = new GridCoordinate(nr, nc);
                        }
                    }
                }
            }

            return bestCoord;
        }

        public GridCoordinate FindNearestEmptyNeighbor(GridCoordinate coord, Vector3 fromWorldPos)
        {
            if (IsEmpty(coord.Row, coord.Column))
                return coord;

            List<GridCoordinate> neighbors = GetNeighbors(coord);
            GridCoordinate best = coord;
            float minDistance = float.MaxValue;

            foreach (var n in neighbors)
            {
                if (IsEmpty(n.Row, n.Column))
                {
                    float dist = Vector3.Distance(fromWorldPos, GridToWorldPosition(n.Row, n.Column));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        best = n;
                    }
                }
            }

            return best;
        }

        // ---------------------------------------------------------------
        // Validation / access
        // ---------------------------------------------------------------

        public bool IsValidCoordinate(int row, int col)
        {
            if (row < 0 || row >= maxRows || col < 0)
                return false;

            return col < GetColumnCountForRow(row);
        }

        public bool IsValidCoordinate(GridCoordinate coord)
        {
            return IsValidCoordinate(coord.Row, coord.Column);
        }

        public bool IsEmpty(int row, int col)
        {
            if (!IsValidCoordinate(row, col))
                return false;
            return grid[row, col] == null;
        }

        /// <summary>
        /// Bubble ko grid ke diameter ke mutabiq scale karta hai (overlap fix).
        /// </summary>
        public void FitBubbleSize(BubbleController bubble)
        {
            if (bubble == null)
                return;

            SpriteRenderer sr = bubble.GetComponentInChildren<SpriteRenderer>();

            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning(
                    "[BubbleGrid] Bubble has no SpriteRenderer or sprite: "
                    + bubble.name
                );
                return;
            }

            // Reset the bubble root scale before measuring its visual size.
            bubble.transform.localScale = Vector3.one;

            float currentSize = sr.bounds.size.x;

            if (currentSize <= 0.0001f)
            {
                Debug.LogWarning(
                    "[BubbleGrid] Invalid bubble sprite size: " + bubble.name
                );
                return;
            }

            // Fit the rendered bubble to the grid's world-space diameter.
            float scaleFactor = bubbleDiameter / currentSize;

            bubble.transform.localScale = Vector3.one * scaleFactor;
        }

        public bool AddBubble(int row, int col, BubbleController bubble)
        {
            if (bubble == null)
            {
                Debug.LogWarning("[BubbleGrid] Cannot add a null bubble.");
                return false;
            }

            if (!IsValidCoordinate(row, col))
            {
                Debug.LogWarning(
                    $"[BubbleGrid] Invalid coordinate ({row}, {col})."
                );
                return false;
            }

            // Do not overwrite another bubble in this cell.
            if (grid[row, col] != null && grid[row, col] != bubble)
            {
                Debug.LogError(
                    $"[BubbleGrid] Cell ({row}, {col}) is already occupied. "
                    + "Bubble was not added: " + bubble.name
                );
                return false;
            }

            // Do not register the same bubble in multiple cells.
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] == bubble &&
                        (r != row || c != col))
                    {
                        Debug.LogError(
                            $"[BubbleGrid] Bubble {bubble.name} is already "
                            + $"registered at ({r}, {c})."
                        );
                        return false;
                    }
                }
            }

            // Register first, then apply the authoritative grid position.
            grid[row, col] = bubble;

            bubble.SetGridPosition(row, col);

            FitBubbleSize(bubble);

            bubble.transform.position = GridToWorldPosition(row, col);

            bubble.Attach();

            return true;
        }

        public BubbleController RemoveBubble(int row, int col)
        {
            if (!IsValidCoordinate(row, col))
                return null;

            BubbleController b = grid[row, col];
            grid[row, col] = null;
            return b;
        }

        public BubbleController GetBubble(int row, int col)
        {
            if (!IsValidCoordinate(row, col))
                return null;

            return grid[row, col];
        }

        public BubbleController GetBubble(GridCoordinate coord)
        {
            return GetBubble(coord.Row, coord.Column);
        }

        public List<GridCoordinate> GetNeighbors(GridCoordinate coord)
        {
            List<GridCoordinate> neighbors = new List<GridCoordinate>();
            int r = coord.Row;
            int c = coord.Column;

            // Horizontal neighbors
            AddIfValid(neighbors, r, c - 1);
            AddIfValid(neighbors, r, c + 1);

            // Square layout: sirf upar aur neeche wali bubble touch karti hai
            if (!staggerRows)
            {
                AddIfValid(neighbors, r - 1, c);
                AddIfValid(neighbors, r + 1, c);
                return neighbors;
            }

            // Up and Down neighbors depend on Even / Odd row parity
            if (!IsOddRow(r)) // Even row
            {
                AddIfValid(neighbors, r - 1, c - 1); // Up-Left
                AddIfValid(neighbors, r - 1, c);     // Up-Right
                AddIfValid(neighbors, r + 1, c - 1); // Down-Left
                AddIfValid(neighbors, r + 1, c);     // Down-Right
            }
            else // Odd row
            {
                AddIfValid(neighbors, r - 1, c);     // Up-Left
                AddIfValid(neighbors, r - 1, c + 1); // Up-Right
                AddIfValid(neighbors, r + 1, c);     // Down-Left
                AddIfValid(neighbors, r + 1, c + 1); // Down-Right
            }

            return neighbors;
        }

        private void AddIfValid(List<GridCoordinate> list, int row, int col)
        {
            if (IsValidCoordinate(row, col))
            {
                list.Add(new GridCoordinate(row, col));
            }
        }

        public List<BubbleController> GetAllBubbles()
        {
            List<BubbleController> list = new List<BubbleController>();
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] != null)
                    {
                        list.Add(grid[r, c]);
                    }
                }
            }
            return list;
        }

        // ---------------------------------------------------------------
        // Colors currently present in the grid (shooter colors ke liye)
        // ---------------------------------------------------------------

        /// <summary>Grid mein is waqt jo colors maujood hain (duplicate ke baghair).</summary>
        public List<BubbleColor> GetColorsInGrid()
        {
            HashSet<BubbleColor> set = new HashSet<BubbleColor>();
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] != null)
                        set.Add(grid[r, c].Color);
                }
            }
            return new List<BubbleColor>(set);
        }

        /// <summary>Kya grid mein is color ki koi bubble maujood hai?</summary>
        public bool HasColor(BubbleColor color)
        {
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] != null && grid[r, c].Color == color)
                        return true;
                }
            }
            return false;
        }

        public void ClearGrid()
        {
            if (grid == null) return;
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] != null)
                    {
                        Destroy(grid[r, c].gameObject);
                        grid[r, c] = null;
                    }
                }
            }
            rowParityShift = 0;
            ResetRowMotionState();
        }

        // ---------------------------------------------------------------
        // Shooting helpers (hit detection + play-area bounds)
        // ---------------------------------------------------------------

        /// <summary>Left wall par shoot hone wali bubble ka center x.</summary>
        public float LeftWallX => gridOrigin.x;

        /// <summary>Right wall par shoot hone wali bubble ka center x.</summary>
        public float RightWallX => gridOrigin.x + (maxColumns - 1) * bubbleDiameter;

        /// <summary>Ceiling: row 0 ka center y. Is tak pohonchne par bubble chipak jati hai.</summary>
        public float CeilingY => gridOrigin.y;

        /// <summary>
        /// Distance-based hit check. pos ke hitRadius ke andar koi bubble ho to return karta hai.
        /// Tez chalne wali bubble beech se skip nahi hoti (physics par depend nahi).
        /// </summary>
        public BubbleController FindHitBubble(Vector3 pos, float hitRadius)
        {
            float sqrRadius = hitRadius * hitRadius;
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    BubbleController b = grid[r, c];
                    if (b == null) continue;

                    Vector2 diff = (Vector2)b.transform.position - (Vector2)pos;
                    if (diff.sqrMagnitude <= sqrRadius)
                        return b;
                }
            }
            return null;
        }
        /// <summary>
        /// Hit wali bubble ke SEEDHE khali padosiyon mein se shot position ke sab se qareeb cell chunta hai.
        /// Is se bubble hamesha usi bubble ke saath judti hai jise wo lagi.
        /// </summary>
        public GridCoordinate FindAttachCellForHit(BubbleController hit, Vector3 shotPos)
        {
            GridCoordinate hc = hit.GridPosition;
            GridCoordinate best = hc;
            float minDist = float.MaxValue;
            bool found = false;

            foreach (GridCoordinate n in GetNeighbors(hc))
            {
                if (!IsEmpty(n.Row, n.Column)) continue;

                float dist = Vector3.Distance(shotPos, GridToWorldPosition(n.Row, n.Column));
                if (dist < minDist)
                {
                    minDist = dist;
                    best = n;
                    found = true;
                }
            }

            // Koi seedha khali padosi na ho to purana tareeqa
            if (!found)
                return FindNearestEmptyNeighbor(WorldToGridPosition(shotPos), shotPos);

            return best;
        }

        // ---------------------------------------------------------------
        // Floating bubbles (pop ke baad lataki hui bubbles gira do)
        // ---------------------------------------------------------------

        /// <summary>
        /// Top row (ceiling) se judi hui bubbles ko BFS se dhundta hai.
        /// Jo judi nahi hain, unke coordinates return karta hai.
        /// </summary>
        public List<GridCoordinate> GetFloatingCoordinates()
        {
            bool[,] visited = new bool[maxRows, maxColumns];
            Queue<GridCoordinate> queue = new Queue<GridCoordinate>();

            for (int c = 0; c < maxColumns; c++)
            {
                if (grid[0, c] != null)
                {
                    visited[0, c] = true;
                    queue.Enqueue(new GridCoordinate(0, c));
                }
            }

            while (queue.Count > 0)
            {
                GridCoordinate cur = queue.Dequeue();
                foreach (var n in GetNeighbors(cur))
                {
                    if (!visited[n.Row, n.Column] && grid[n.Row, n.Column] != null)
                    {
                        visited[n.Row, n.Column] = true;
                        queue.Enqueue(n);
                    }
                }
            }

            List<GridCoordinate> floating = new List<GridCoordinate>();
            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    if (grid[r, c] != null && !visited[r, c])
                    {
                        floating.Add(new GridCoordinate(r, c));
                    }
                }
            }
            return floating;
        }

        /// <summary>
        /// Floating bubbles ko grid se hata kar neeche girata hai.
        /// Return: kitni bubbles giri (score ke liye).
        /// Matching bubbles pop karne ke foran baad is ko call karo.
        /// </summary>
        public int DropFloatingBubbles()
        {
            List<GridCoordinate> floating = GetFloatingCoordinates();

            foreach (var coord in floating)
            {
                BubbleController b = RemoveBubble(coord.Row, coord.Column);
                if (b != null)
                {
                    Vector2 velocity = new Vector2(Random.Range(-2f, 2f), Random.Range(1f, 3f));
                    b.Fall(velocity);
                }
            }

            return floating.Count;
        }

        // ---------------------------------------------------------------
        // Horizontal row motion (moving targets)
        // ---------------------------------------------------------------

        /// <summary>
        /// Level ke liye rows ki horizontal motion on/off karo.
        /// speed = world units per second. alternate = ek row dayen, agli bayen.
        /// </summary>
        public void SetRowMotion(bool enabled, float speed, bool alternateDirections)
        {
            rowMotionEnabled = enabled && speed > 0f;
            rowMotionSpeed = Mathf.Max(0f, speed);
            alternateRowDirections = alternateDirections;
            ResetRowMotionState();
        }

        private void ResetRowMotionState()
        {
            sharedOffset = 0f;
            sharedDirection = 1f;

            if (rowOffsets == null || rowDirections == null) return;

            for (int r = 0; r < rowOffsets.Length; r++)
            {
                rowOffsets[r] = 0f;
                rowDirections[r] = (alternateRowDirections && r % 2 == 1) ? -1f : 1f;
            }
        }

        /// <summary>Is row ka abhi ka horizontal offset (motion ki wajah se).</summary>
        private float GetRowMotionOffset(int row)
        {
            if (rowOffsets == null || row < 0 || row >= rowOffsets.Length)
                return 0f;

            return rowOffsets[row];
        }

        private void Update()
        {
            if (!rowMotionEnabled || rowMotionPaused || grid == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // game pause (timeScale 0)

            UpdateRowMotion(dt);
        }

        /// <summary>
        /// Har row apni bubbles ke hisab se left wall se right wall ke beech ping-pong karti hai.
        /// Jis row ki bubbles poori chaurai bhar den, wo hil nahi sakti (jagah nahi).
        /// </summary>
        private void UpdateRowMotion(float dt)
        {
            float d = bubbleDiameter;

            // Pehle sab rows ki hadein nikalo
            float[] lowers = new float[maxRows];
            float[] uppers = new float[maxRows];
            bool[] hasBubbles = new bool[maxRows];

            float globalLower = float.MaxValue;
            float globalUpper = float.MinValue;

            for (int r = 0; r < maxRows; r++)
            {
                int minCol = int.MaxValue;
                int maxCol = -1;
                int cols = GetColumnCountForRow(r);

                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c] == null) continue;
                    if (c < minCol) minCol = c;
                    if (c > maxCol) maxCol = c;
                }

                if (maxCol < 0) continue;   // khali row

                float stagger = IsOddRow(r) ? d * 0.5f : 0f;
                lowers[r] = LeftWallX - (gridOrigin.x + minCol * d + stagger);
                uppers[r] = RightWallX - (gridOrigin.x + maxCol * d + stagger);
                hasBubbles[r] = true;

                if (lowers[r] < globalLower) globalLower = lowers[r];
                if (uppers[r] > globalUpper) globalUpper = uppers[r];
            }

            // Shared mode: ek hi offset, ek hi direction
            if (!alternateRowDirections && globalUpper - globalLower > 0.001f)
            {
                sharedOffset += sharedDirection * rowMotionSpeed * dt;

                if (sharedOffset >= globalUpper)
                {
                    sharedOffset = globalUpper;
                    sharedDirection = -1f;
                }
                else if (sharedOffset <= globalLower)
                {
                    sharedOffset = globalLower;
                    sharedDirection = 1f;
                }
            }

            for (int r = 0; r < maxRows; r++)
            {
                if (!hasBubbles[r]) continue;

                float offset;

                if (alternateRowDirections)
                {
                    // Purana tareeqa: har row apni direction mein ping-pong karti hai
                    offset = rowOffsets[r];

                    if (uppers[r] - lowers[r] < 0.001f)
                    {
                        offset = lowers[r];
                    }
                    else
                    {
                        offset += rowDirections[r] * rowMotionSpeed * dt;

                        if (offset >= uppers[r])
                        {
                            offset = uppers[r];
                            rowDirections[r] = -1f;
                        }
                        else if (offset <= lowers[r])
                        {
                            offset = lowers[r];
                            rowDirections[r] = 1f;
                        }
                    }
                }
                else
                {
                    // Shared offset, apni hadon ke andar clamp (ruk sakti hai, ulti nahi chalti)
                    offset = Mathf.Clamp(sharedOffset, lowers[r], uppers[r]);
                }

                rowOffsets[r] = offset;

                int cols = GetColumnCountForRow(r);
                for (int c = 0; c < cols; c++)
                {
                    BubbleController b = grid[r, c];
                    if (b != null)
                        b.transform.position = GridToWorldPosition(r, c);
                }
            }
        }
        // ---------------------------------------------------------------
        // New rows from the top
        // ---------------------------------------------------------------

        /// <summary>
        /// Saari bubbles ko 1 row neeche shift karta hai (seedhi, sideways nahi).
        /// false return karta hai agar aakhri row mein bubble thi (GAME OVER).
        /// Shift ke baad row 0 khali hoti hai, wahan nayi bubbles spawn karo.
        /// </summary>
        public bool ShiftRowsDown()
        {
            // Game over check: aakhri row mein bubble hai to neeche jagah nahi
            for (int c = 0; c < maxColumns; c++)
            {
                if (grid[maxRows - 1, c] != null)
                    return false;
            }

            // Parity flip: is se har bubble ki x position same rehti hai
            rowParityShift = 1 - rowParityShift;

            // Har row ka offset/direction bhi row ke saath ek row neeche chala jaye
            if (rowOffsets != null)
            {
                for (int r = maxRows - 1; r >= 1; r--)
                {
                    rowOffsets[r] = rowOffsets[r - 1];
                    rowDirections[r] = rowDirections[r - 1];
                }

                rowOffsets[0] = 0f;
                rowDirections[0] = alternateRowDirections ? -rowDirections[1] : rowDirections[1];
            }

            for (int r = maxRows - 2; r >= 0; r--)
            {
                for (int c = 0; c < maxColumns; c++)
                {
                    BubbleController b = grid[r, c];
                    if (b == null) continue;

                    grid[r + 1, c] = b;
                    grid[r, c] = null;
                    b.SetGridPosition(r + 1, c);
                    b.transform.position = GridToWorldPosition(r + 1, c);
                }
            }

            return true;
        }
    }
}