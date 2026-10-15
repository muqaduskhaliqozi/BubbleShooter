using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.UI;

namespace BubbleShooter.Level
{
    /// <summary>
    /// Kis level mein bubbles ki rows horizontally (left-right) hilti rahen.
    /// speed = world units per second (chhota number = slow).
    /// </summary>
    [Serializable]
    public class LevelMotionEntry
    {
        public BubbleLevelData level;

        [Tooltip("Rows ki raftaar (units/second). 0.2 bohot slow, 0.4 slow, 0.8 theek-thaak tez.")]
        [Min(0f)] public float speed = 0.4f;

        [Tooltip("ON: ek row dayen to agli row bayen. OFF: sab rows ek hi taraf se shuru.")]
        public bool alternateDirections = true;
    }

    public class LevelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BubbleGrid bubbleGrid;
        [SerializeField] private GameObject bubblePrefab;

        [Header("UI")]
        [SerializeField] private GameplayUI gameplayUI;
        [Tooltip("ON: LevelManager khud GameplayUI ke buttons handle karega. " +
                 "Agar GameManager pehle se in events par subscribe hai to isay OFF karo, warna double kaam hoga.")]
        [SerializeField] private bool handleUIEvents = true;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Level Assets - Assign In Order")]
        [SerializeField] private BubbleLevelData[] levels;
        [SerializeField] private BubbleLevelData currentLevelData;
        [Header("Debug (sirf Editor mein)")]
        [Tooltip("ON karo to game seedha neeche likhe level se shuru hoga.")]
        [SerializeField] private bool useDebugStartLevel = false;
        [Tooltip("1 = pehla level, 6 = chhatha level.")]
        [SerializeField] private int debugStartLevelNumber = 6;

        [Header("Level Shapes (optional)")]
        [Tooltip("Jis level ka shape yahan likha ho, uski starting bubbles us shape mein lagti hain. " +
                 "Jo level yahan na ho, uski normal seedhi rows (startingRows x activeColumns) banti hain.")]
        [SerializeField] private LevelShapeEntry[] levelShapes;
        [Tooltip("ON: shape ki apni rows aane ke baad bhi pending rows baaqi hon to wohi shape dobara " +
                 "upar se aata rahe (ek ke upar ek). OFF: baaqi rows simple (activeColumns wali) aati hain. " +
                 "Kitni rows aayengi ye LevelData ke pendingRows se tay hota hai.")]
        [SerializeField] private bool repeatShapeForPendingRows = true;

        [Header("Row Motion (moving targets)")]
        [Tooltip("Jis level ko yahan likha jaye, uski bubbles ki rows slow-slow left-right hilti hain. " +
                 "Jo level yahan na ho, uski bubbles sthir rehti hain. " +
                 "Rows ko hilne ke liye jagah chahiye: level mein activeColumns grid ke Max Columns se kam rakho.")]
        [SerializeField] private LevelMotionEntry[] levelMotions;

        [Header("Color Layout")]
        [Tooltip("Ek jaisi bubbles ka gucha (group) zyada se zyada kitna bada ho. " +
                 "Shuru mein har bubble kam az kam 1 saathi ke saath hoti hai, koi akeli nahi.")]
        [Range(2, 6)]
        [SerializeField] private int maxGroupSize = 4;
        [Tooltip("Kitni sambhavna se nayi bubble padosi ke color mein judti hai (zyada = zyada groups).")]
        [Range(0f, 1f)]
        [SerializeField] private float groupChance = 0.85f;

        // Shape ki baad mein aane wali rows (aane ki tarteeb mein) aur shape ki chaurai
        private readonly Queue<string> pendingShapeLines = new Queue<string>();
        private int shapeWidth;
        // Poore shape ki rows, aane ki tarteeb mein (sab se neeche wali row pehle). Shape dobara dohrane ke liye.
        private readonly List<string> shapeLoopLines = new List<string>();

        private int currentLevelIndex;
        private int pendingRowsRemaining;
        private int shotsSinceLastRow;
        private bool rowAnimationPlaying;
        private bool uiSubscribed;

        public BubbleLevelData CurrentLevelData => currentLevelData;
        /// <summary>Level load / retry ke baad fire hota hai (GameManager state reset ke liye use karo).</summary>
        public event Action<BubbleLevelData> OnLevelLoaded;

        public int PendingRowsRemaining => pendingRowsRemaining;
        public bool IsAnimatingRow => rowAnimationPlaying;

        /// <summary>Kya is level ke baad aur level maujood hai?</summary>
        public bool HasNextLevel => levels != null && currentLevelIndex + 1 < levels.Length;

        public void Initialize(
    BubbleGrid grid,
    GameObject prefab,
    BubbleLevelData levelData = null)
        {
            bubbleGrid = grid;
            bubblePrefab = prefab;

            if (levelData != null)
                currentLevelData = levelData;

            if (levels != null && levels.Length > 0)
            {
                currentLevelIndex = Array.IndexOf(levels, currentLevelData);

                if (currentLevelIndex < 0)
                    currentLevelIndex = 0;

#if UNITY_EDITOR
                if (useDebugStartLevel)
                {
                    currentLevelIndex = Mathf.Clamp(debugStartLevelNumber - 1, 0, levels.Length - 1);
                    currentLevelData = levels[currentLevelIndex];
                }
#endif
            }
        }

        public void LoadLevel(BubbleLevelData levelData = null)
        {
            if (levelData != null)
            {
                currentLevelData = levelData;

                if (levels != null)
                {
                    int index = Array.IndexOf(levels, levelData);
                    if (index >= 0)
                        currentLevelIndex = index;
                }
            }
            else if (currentLevelData == null &&
                     levels != null && levels.Length > 0)
            {
                currentLevelIndex = Mathf.Clamp(
                    currentLevelIndex, 0, levels.Length - 1);

                currentLevelData = levels[currentLevelIndex];
            }

            if (bubbleGrid == null || bubblePrefab == null ||
                currentLevelData == null)
            {
                Debug.LogError(
                    "[LevelManager] Assign Grid, Prefab and Level Data.");
                return;
            }

            // Do not resize the grid for individual levels.
            bubbleGrid.ClearGrid();

            // Is level mein rows hilni chahiyen ya nahi
            ApplyRowMotionFor(currentLevelData);

            pendingRowsRemaining =
                Mathf.Max(0, currentLevelData.pendingRows);

            shotsSinceLastRow = 0;
            rowAnimationPlaying = false;

            pendingShapeLines.Clear();
            shapeLoopLines.Clear();
            shapeWidth = 0;

            LevelShapeData shape = GetShapeFor(currentLevelData);

            if (shape != null)
            {
                // Is level ka khaas shape (teen-kona, dil, ring, ...)
                SpawnShape(shape);

                Debug.Log(
                    $"Level {currentLevelData.levelNumber}: shape '{shape.name}', " +
                    $"{pendingRowsRemaining} pending rows.");
            }
            else
            {
                // Normal seedhi rows
                int rows = Mathf.Clamp(
                    currentLevelData.startingRows, 1,
                    bubbleGrid.MaxRows - 1);

                for (int row = 0; row < rows; row++)
                {
                    int validColumns =
                        bubbleGrid.GetColumnCountForRow(row);

                    int active = Mathf.Clamp(
                        currentLevelData.activeColumns, 1,
                        validColumns);

                    // Center the active bubbles in each valid row.
                    int startColumn = (validColumns - active) / 2;

                    for (int i = 0; i < active; i++)
                    {
                        int col = startColumn + i;
                        BubbleColor color = GetSafeRandomColor(row, col);

                        SpawnBubble(row, col, color);
                    }
                }

                Debug.Log(
                    $"Level {currentLevelData.levelNumber}: " +
                    $"{rows} starting rows, " +
                    $"{currentLevelData.activeColumns} active columns, " +
                    $"{pendingRowsRemaining} pending rows.");
            }

            // Koi bubble akeli na rahe
            FixSingletons(GetAllBubbleCoordinates());

            if (gameplayUI != null)
                gameplayUI.UpdateLevelText(currentLevelData.levelNumber);

            OnLevelLoaded?.Invoke(currentLevelData);
        }

        // =====================================================
        //  ROW MOTION (horizontal moving targets)
        // =====================================================

        private LevelMotionEntry GetMotionEntryFor(BubbleLevelData level)
        {
            if (levelMotions == null || level == null) return null;

            foreach (LevelMotionEntry entry in levelMotions)
            {
                if (entry != null && entry.level == level)
                    return entry;
            }
            return null;
        }

        private void ApplyRowMotionFor(BubbleLevelData level)
        {
            LevelMotionEntry motion = GetMotionEntryFor(level);

            if (motion != null && motion.speed > 0f)
            {
                bubbleGrid.SetRowMotion(true, motion.speed, motion.alternateDirections);
                Debug.Log($"[LevelManager] Level {level.levelNumber}: rows horizontally hil rahi hain (speed {motion.speed}).");
            }
            else
            {
                bubbleGrid.SetRowMotion(false, 0f, false);
            }
        }

        private void Update()
        {
            // Nayi row ki slide-in animation chal rahi ho to rows ki motion rok do
            // (dono ek hi waqt positions badlen to takrao hota hai)
            if (bubbleGrid != null)
                bubbleGrid.RowMotionPaused = rowAnimationPlaying;
        }

        // =====================================================
        //  SHAPES
        // =====================================================

        private LevelShapeData GetShapeFor(BubbleLevelData level)
        {
            if (levelShapes == null || level == null) return null;

            foreach (LevelShapeEntry entry in levelShapes)
            {
                if (entry != null && entry.level == level && entry.shape != null)
                    return entry.shape;
            }
            return null;
        }

        private void SpawnShape(LevelShapeData shape)
        {
            string[] initialLines;
            string[] pendingLines;
            shape.GetParts(out initialLines, out pendingLines);

            if (initialLines.Length == 0)
            {
                Debug.LogWarning($"[LevelManager] Shape '{shape.name}' mein shuru ki koi row nahi hai.");
                return;
            }

            shapeWidth = shape.MaxLineLength;

            // Poora shape neeche se upar (aane ki tarteeb mein): dobara dohrane ke liye yaad rakho
            string[] fullLines = shape.GetLines();
            for (int i = fullLines.Length - 1; i >= 0; i--)
                shapeLoopLines.Add(fullLines[i]);

            if (shapeWidth > bubbleGrid.GetColumnCountForRow(0))
            {
                Debug.LogWarning(
                    $"[LevelManager] Shape '{shape.name}' ki chaurai ({shapeWidth}) " +
                    $"grid ke columns ({bubbleGrid.GetColumnCountForRow(0)}) se zyada hai. Kinare kat jayenge.");
            }

            // Shuru ki rows (separator ke neeche wali, ya sab agar separator nahi)
            int rows = Mathf.Min(initialLines.Length, bubbleGrid.MaxRows - 1);
            for (int row = 0; row < rows; row++)
                SpawnShapeLine(initialLines[row], row, null, null);

            // Baad mein upar se aane wali rows
            foreach (string line in pendingLines)
                pendingShapeLines.Enqueue(line);

            pendingRowsRemaining += pendingLines.Length;

            // Jo bubbles upar (row 0) se judi nahi, wo floating hain.
            List<GridCoordinate> floating = bubbleGrid.GetFloatingCoordinates();
            if (floating.Count > 0)
            {
                Debug.LogWarning(
                    $"[LevelManager] Shape '{shape.name}' mein {floating.Count} bubbles upar se judi nahi hain. " +
                    "Pehle pop par ye gir jayengi. Shape ko aise badlo ke har bubble row 0 se judi ho " +
                    "(square layout mein seedhi jurni chahiye, tirchi nahi).");
            }
        }

        /// <summary>
        /// Shape ki ek line ko diye gaye row par spawn karta hai (beech mein rakh kar).
        /// Spawn hui bubbles aur unke cells spawned/cells lists mein bhi daal deta hai (agar diye hon).
        /// false return karta hai agar koi cell pehle se bhara ho (jagah nahi).
        /// </summary>
        private bool SpawnShapeLine(string line, int row, List<BubbleController> spawned, List<GridCoordinate> cells)
        {
            int validColumns = bubbleGrid.GetColumnCountForRow(row);
            int offset = Mathf.Max(0, (validColumns - shapeWidth) / 2);

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];

                if (ch == '.' || ch == ' ' || ch == '_')
                    continue;

                int col = offset + i;
                if (col >= validColumns)
                    continue;

                if (!bubbleGrid.IsEmpty(row, col))
                    return false;

                BubbleColor color;
                if (!TryGetFixedColor(ch, out color))
                    color = GetSafeRandomColor(row, col);

                SpawnBubble(row, col, color);

                BubbleController b = bubbleGrid.GetBubble(row, col);
                if (b != null)
                {
                    if (spawned != null) spawned.Add(b);
                    if (cells != null) cells.Add(new GridCoordinate(row, col));
                }
            }

            return true;
        }

        /// <summary>
        /// Upar (row 0) par agli row spawn karta hai: pehle shape ki baqi rows, phir normal rows
        /// (activeColumns wali). Nayi bubbles screen ke upar se start hoti hain (slide-in ke liye).
        /// false = jagah nahi mili (game over).
        /// </summary>
        private bool SpawnNextRowAtTop(List<BubbleController> spawned, List<GridCoordinate> cells)
        {
            bool ok = true;

            // Shape ki rows khatam ho gayin magar pending rows baaqi hain: wohi shape dobara shuru karo
            if (pendingShapeLines.Count == 0 && repeatShapeForPendingRows && shapeLoopLines.Count > 0)
            {
                foreach (string line in shapeLoopLines)
                    pendingShapeLines.Enqueue(line);
            }

            if (pendingShapeLines.Count > 0)
            {
                ok = SpawnShapeLine(pendingShapeLines.Dequeue(), 0, spawned, cells);
            }
            else
            {
                int validColumns = bubbleGrid.GetColumnCountForRow(0);
                int active = Mathf.Clamp(
                    currentLevelData.activeColumns, 1, validColumns);

                int startColumn = (validColumns - active) / 2;

                for (int i = 0; i < active; i++)
                {
                    int col = startColumn + i;

                    if (!bubbleGrid.IsEmpty(0, col))
                    {
                        ok = false;
                        break;
                    }

                    SpawnBubble(0, col, GetSafeRandomColor(0, col));

                    BubbleController b = bubbleGrid.GetBubble(0, col);
                    if (b != null)
                    {
                        spawned.Add(b);
                        cells.Add(new GridCoordinate(0, col));
                    }
                }
            }

            if (!ok) return false;

            // Nayi bubbles ko visually upar se start karao
            foreach (BubbleController b in spawned)
            {
                Vector3 target = bubbleGrid.GridToWorldPosition(
                    b.GridPosition.Row, b.GridPosition.Column);

                b.transform.position = target + Vector3.up * 0.9f;
            }

            return true;
        }

        /// <summary>'1' se '9' = level ke colors ki list ka index (1 = pehla color).</summary>
        private bool TryGetFixedColor(char ch, out BubbleColor color)
        {
            color = BubbleColor.Red;

            if (ch < '1' || ch > '9') return false;

            List<BubbleColor> palette = currentLevelData.availableColors;
            int index = ch - '1';

            if (palette == null || index >= palette.Count) return false;

            color = palette[index];
            return true;
        }

        // =====================================================

        /// <summary>
        /// Is cell ke liye color chunta hai. Koshish: padosi ke color mein judo (group banao),
        /// magar group maxGroupSize se bada na ho.
        /// </summary>
        private BubbleColor GetSafeRandomColor(int row, int col)
        {
            List<BubbleColor> palette =
                currentLevelData.availableColors;

            if (palette == null || palette.Count == 0)
                return BubbleColor.Red;

            List<BubbleColor> groupOptions = new List<BubbleColor>();   // padosi se judkar group (2..max)
            List<BubbleColor> singleOptions = new List<BubbleColor>();  // kisi se na jude

            BubbleColor smallestClusterColor = palette[0];
            int smallestCluster = int.MaxValue;

            foreach (BubbleColor candidate in palette)
            {
                int size = GetClusterSizeIfPlaced(row, col, candidate);

                if (size < smallestCluster)
                {
                    smallestCluster = size;
                    smallestClusterColor = candidate;
                }

                if (size > maxGroupSize) continue;   // gucha bohot bada ho jayega

                if (size >= 2) groupOptions.Add(candidate);
                else singleOptions.Add(candidate);
            }

            bool wantGroup = UnityEngine.Random.value < groupChance;

            if (wantGroup && groupOptions.Count > 0)
                return groupOptions[UnityEngine.Random.Range(0, groupOptions.Count)];

            if (singleOptions.Count > 0)
                return singleOptions[UnityEngine.Random.Range(0, singleOptions.Count)];

            if (groupOptions.Count > 0)
                return groupOptions[UnityEngine.Random.Range(0, groupOptions.Count)];

            return smallestClusterColor;
        }

        /// <summary>Grid ki sab bubbles ke coordinates.</summary>
        private List<GridCoordinate> GetAllBubbleCoordinates()
        {
            List<GridCoordinate> coords = new List<GridCoordinate>();
            foreach (BubbleController b in bubbleGrid.GetAllBubbles())
            {
                if (b != null) coords.Add(b.GridPosition);
            }
            return coords;
        }

        /// <summary>
        /// Jo bubble apne color mein akeli reh gayi (koi same-color padosi nahi),
        /// usko padosi ke color mein badal deta hai taake wo us group ka hissa ban jaye.
        /// Is se level mein koi bubble akeli nahi rehti.
        /// </summary>
        private void FixSingletons(List<GridCoordinate> cells)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;

                foreach (GridCoordinate cell in cells)
                {
                    BubbleController bubble = bubbleGrid.GetBubble(cell);
                    if (bubble == null) continue;

                    // Pehle se kisi group mein hai?
                    if (GetClusterSizeIfPlaced(cell.Row, cell.Column, bubble.Color) > 1)
                        continue;

                    // Padosiyon ke colors mein se wo chuno jo group bada na kare (warna sab se chhota)
                    List<BubbleColor> fitting = new List<BubbleColor>();
                    BubbleColor smallestColor = bubble.Color;
                    int smallestSize = int.MaxValue;
                    bool anyNeighbor = false;

                    foreach (GridCoordinate n in bubbleGrid.GetNeighbors(cell))
                    {
                        BubbleController neighbor = bubbleGrid.GetBubble(n);
                        if (neighbor == null) continue;

                        anyNeighbor = true;
                        BubbleColor c = neighbor.Color;
                        int size = GetClusterSizeIfPlaced(cell.Row, cell.Column, c);

                        if (size <= maxGroupSize && !fitting.Contains(c))
                            fitting.Add(c);

                        if (size < smallestSize)
                        {
                            smallestSize = size;
                            smallestColor = c;
                        }
                    }

                    if (!anyNeighbor) continue;   // bilkul akeli jagah, kuch nahi ho sakta

                    BubbleColor chosen = fitting.Count > 0
                        ? fitting[UnityEngine.Random.Range(0, fitting.Count)]
                        : smallestColor;

                    if (chosen != bubble.Color)
                    {
                        bubble.SetColor(chosen);
                        changed = true;
                    }
                }

                if (!changed) break;
            }
        }

        /// <summary>
        /// Agar is cell mein 'color' ki bubble lagayen to uska same-color gucha kitna bada hoga?
        /// (padosiyon ke guchon samet, 1 = akeli)
        /// </summary>
        private int GetClusterSizeIfPlaced(int row, int col, BubbleColor color)
        {
            HashSet<GridCoordinate> visited = new HashSet<GridCoordinate>();
            int size = 1;

            foreach (GridCoordinate n in bubbleGrid.GetNeighbors(new GridCoordinate(row, col)))
            {
                BubbleController neighbor = bubbleGrid.GetBubble(n);

                if (neighbor != null && neighbor.Color == color && !visited.Contains(n))
                    size += CountCluster(n, color, visited);
            }

            return size;
        }

        /// <summary>Start se judi hui same-color bubbles ginta hai (flood fill).</summary>
        private int CountCluster(GridCoordinate start, BubbleColor color, HashSet<GridCoordinate> visited)
        {
            int count = 0;
            Stack<GridCoordinate> stack = new Stack<GridCoordinate>();

            visited.Add(start);
            stack.Push(start);

            while (stack.Count > 0)
            {
                GridCoordinate cur = stack.Pop();
                count++;

                foreach (GridCoordinate n in bubbleGrid.GetNeighbors(cur))
                {
                    if (visited.Contains(n)) continue;

                    BubbleController b = bubbleGrid.GetBubble(n);
                    if (b != null && b.Color == color)
                    {
                        visited.Add(n);
                        stack.Push(n);
                    }
                }
            }

            return count;
        }

        private void SpawnBubble(int row, int col, BubbleColor color)
        {
            GameObject obj = Instantiate(
                bubblePrefab, bubbleGrid.transform);

            BubbleController controller =
                obj.GetComponent<BubbleController>();

            if (controller == null)
            {
                Destroy(obj);
                Debug.LogError(
                    "[LevelManager] Prefab needs BubbleController.");
                return;
            }

            controller.SetColor(color);

            if (!bubbleGrid.AddBubble(row, col, controller))
                Destroy(obj);
        }

        /// <summary>
        /// Har shot ke baad chalao. shotsPerNewRow shots ho jayein (ya forceRow true ho)
        /// to saari bubbles ek row neeche aati hain aur nayi row upar se girti hai.
        /// onComplete(false) = game over (neeche jagah nahi bachi).
        /// </summary>
        public IEnumerator ResolveShotAndMaybeAddRow(
            Action<bool> onComplete,
            bool forceRow = false)
        {
            if (rowAnimationPlaying || currentLevelData == null)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (pendingRowsRemaining <= 0)
            {
                onComplete?.Invoke(true);
                yield break;
            }

            shotsSinceLastRow++;

            if (!forceRow &&
                shotsSinceLastRow <
                Mathf.Max(1, currentLevelData.shotsPerNewRow))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            shotsSinceLastRow = 0;
            rowAnimationPlaying = true;

            // Preserve positions so ShiftRowsDown can be animated.
            List<BubbleController> existing =
                bubbleGrid.GetAllBubbles();

            Dictionary<BubbleController, Vector3> oldPositions =
                new Dictionary<BubbleController, Vector3>();

            foreach (BubbleController b in existing)
            {
                if (b != null)
                    oldPositions[b] = b.transform.position;
            }

            if (!bubbleGrid.ShiftRowsDown())
            {
                rowAnimationPlaying = false;
                onComplete?.Invoke(false);
                yield break;
            }

            const float duration = 0.25f;

            foreach (BubbleController b in existing)
            {
                if (b != null && oldPositions.ContainsKey(b))
                    b.transform.position = oldPositions[b];
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (BubbleController b in existing)
                {
                    if (b == null) continue;

                    Vector3 target = bubbleGrid.GridToWorldPosition(
                        b.GridPosition.Row, b.GridPosition.Column);

                    b.transform.position = Vector3.Lerp(
                        oldPositions[b], target, t);
                }

                yield return null;
            }

            foreach (BubbleController b in existing)
            {
                if (b != null)
                    b.transform.position = bubbleGrid.GridToWorldPosition(
                        b.GridPosition.Row, b.GridPosition.Column);
            }

            List<BubbleController> newBubbles = new List<BubbleController>();
            List<GridCoordinate> newRowCells = new List<GridCoordinate>();

            if (!SpawnNextRowAtTop(newBubbles, newRowCells))
            {
                rowAnimationPlaying = false;
                onComplete?.Invoke(false);
                yield break;
            }

            // Nayi row ki bhi koi bubble akeli na rahe
            FixSingletons(newRowCells);

            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (BubbleController b in newBubbles)
                {
                    if (b == null) continue;

                    Vector3 target = bubbleGrid.GridToWorldPosition(
                        b.GridPosition.Row, b.GridPosition.Column);

                    b.transform.position = Vector3.Lerp(
                        target + Vector3.up * 0.9f, target, t);
                }

                yield return null;
            }

            // Animation ke baad bubbles ki final position pakki karo
            foreach (BubbleController b in newBubbles)
            {
                if (b != null)
                    b.transform.position = bubbleGrid.GridToWorldPosition(
                        b.GridPosition.Row, b.GridPosition.Column);
            }

            pendingRowsRemaining--;
            rowAnimationPlaying = false;
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Win: grid mein 2 ya kam bubbles bachi hon AUR nayi rows aani baaqi na hon.
        /// </summary>
        public bool CheckWinCondition()
        {
            if (bubbleGrid == null)
                return false;

            if (pendingRowsRemaining > 0)
                return false;

            return bubbleGrid.GetAllBubbles().Count <= 2;
        }

        public bool CheckLoseCondition()
        {
            if (bubbleGrid == null || currentLevelData == null)
                return false;

            foreach (BubbleController b in bubbleGrid.GetAllBubbles())
            {
                if (b != null &&
                    b.GridPosition.Row >= currentLevelData.dangerRow)
                    return true;
            }

            return false;
        }

        /// <summary>Agle level par jao aur load karo. Agla level na ho to false.</summary>
        public bool LoadNextLevel()
        {
            if (levels == null || currentLevelIndex + 1 >= levels.Length)
                return false;

            currentLevelIndex++;
            currentLevelData = levels[currentLevelIndex];
            LoadLevel(currentLevelData);
            return true;
        }

        /// <summary>Level 1 (list ka pehla level) load karo.</summary>
        public void LoadFirstLevel()
        {
            if (levels == null || levels.Length == 0)
            {
                LoadLevel(currentLevelData);
                return;
            }

            currentLevelIndex = 0;
            currentLevelData = levels[0];
            LoadLevel(currentLevelData);
        }

        public void ReloadCurrentLevel()
        {
            LoadLevel(currentLevelData);
        }

        // =====================================================
        //  UI PANELS + BUTTONS
        // =====================================================

        private void OnEnable()
        {
            SubscribeUI();
        }

        private void OnDisable()
        {
            UnsubscribeUI();
        }

        private void SubscribeUI()
        {
            if (!handleUIEvents || uiSubscribed) return;

            if (gameplayUI == null)
            {
                Debug.LogWarning("[LevelManager] GameplayUI assign nahi hai, buttons kaam nahi karenge.");
                return;
            }

            gameplayUI.OnNextLevelClicked += OnNextLevelButton;
            gameplayUI.OnRetryClicked += OnRetryButton;
            gameplayUI.OnHomeClicked += OnHomeButton;
            gameplayUI.OnPauseClicked += OnPauseButton;
            gameplayUI.OnResumeClicked += OnResumeButton;
            uiSubscribed = true;
        }

        private void UnsubscribeUI()
        {
            if (!uiSubscribed || gameplayUI == null) return;

            gameplayUI.OnNextLevelClicked -= OnNextLevelButton;
            gameplayUI.OnRetryClicked -= OnRetryButton;
            gameplayUI.OnHomeClicked -= OnHomeButton;
            gameplayUI.OnPauseClicked -= OnPauseButton;
            gameplayUI.OnResumeClicked -= OnResumeButton;
            uiSubscribed = false;
        }

        /// <summary>Level complete hone par call karo (score optional).</summary>
        public void ShowWinPanel(int finalScore = 0)
        {
            Time.timeScale = 1f;
            if (gameplayUI != null) gameplayUI.ShowWinPanel(finalScore);
        }

        /// <summary>Game over hone par call karo (score optional).</summary>
        public void ShowLosePanel(int finalScore = 0)
        {
            Time.timeScale = 1f;
            if (gameplayUI != null) gameplayUI.ShowLosePanel(finalScore);
        }

        private void ResetForNewRun()
        {
            StopAllCoroutines();
            rowAnimationPlaying = false;
            Time.timeScale = 1f;
            if (gameplayUI != null) gameplayUI.HideAllPanels();
        }

        /// <summary>Win panel -> Next button.</summary>
        public void OnNextLevelButton()
        {
            ResetForNewRun();

            if (!LoadNextLevel())
            {
                // Aakhri level thi -> main menu
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        /// <summary>Win / Lose / Pause panel -> Retry button.</summary>
        public void OnRetryButton()
        {
            ResetForNewRun();
            ReloadCurrentLevel();
        }

        /// <summary>Win / Lose / Pause panel -> Home button.</summary>
        public void OnHomeButton()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>Pause panel -> Resume button.</summary>
        public void OnResumeButton()
        {
            if (gameplayUI != null) gameplayUI.ShowPausePanel(false);
            Time.timeScale = 1f;
        }

        /// <summary>TopBar ke PauseButton.</summary>
        public void OnPauseButton()
        {
            if (gameplayUI != null) gameplayUI.ShowPausePanel(true);
            Time.timeScale = 0f;
        }
    }
}