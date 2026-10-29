using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.Shooter;
using BubbleShooter.Matching;
using BubbleShooter.Level;
using BubbleShooter.Score;
using BubbleShooter.UI;
using BubbleShooter.FX;

namespace BubbleShooter.Core
{
    public class BubbleShooterGameManager : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Loading;

        [Header("Subsystem References")]
        [SerializeField] private BubbleGrid bubbleGrid;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private ShooterController shooterController;
        [SerializeField] private AimController aimController;
        [SerializeField] private BubbleMatchFinder matchFinder;
        [SerializeField] private FloatingBubbleDetector floatingDetector;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameplayUI gameplayUI;
        [SerializeField] private BubbleColorGenerator colorGenerator;
        [SerializeField] private GameObject bubblePrefab;
        [SerializeField] private MissIndicatorUI missIndicator;

        [Header("Gameplay Bounds")]
        [Tooltip("ON: walls and ceiling are calculated from BubbleGrid.")]
        [SerializeField] private bool useGridBounds = true;
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float ceilingY = 3.8f;

        [Header("Miss Rule")]
        [Tooltip("Agar kisi level mein Maximum Missed Shots 0 ho to yeh value use hogi. " +
                 "Level ki apni value 0 se zyada ho to wohi chalegi.")]
        [SerializeField] private int fallbackMaxMisses = 12;

        [Tooltip("ON: a successful match resets the consecutive miss counter.")]
        [SerializeField] private bool resetMissesOnMatch = false;

        [Tooltip("ON: a shot that does not match is removed from the grid.")]
        [SerializeField] private bool removeMissedBubble = true;

        [Header("Danger Line")]
        [Tooltip("Khatre ki lakeer shooter se kitni upar ho (bubble diameter ke hisaab se). " +
                 "Koi bubble is lakeer tak pohanche to level fail.")]
        [SerializeField] private float dangerLineDiameters = 0.6f;

        [Header("Juice")]
        [SerializeField] private bool enableEffects = true;

        private int missCount = 0;
        private bool eventsBound = false;
        private bool swapPressActive;

        // Pause se pehle wali state (resume par wapas). Pehle hamesha Ready ho jati thi,
        // jis se udti bubble ke dauran dobara shot chal jata tha.
        private GameState stateBeforePause = GameState.Ready;
        private bool firstShotTaken;
        private float dangerLineY = -3f;
        private GameplayBackdrop backdrop;

        public GameState CurrentState => currentState;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            InitializeGameplay();
        }

        private void ValidateReferences()
        {
            if (bubbleGrid == null)
                Debug.LogError("[BubbleShooter] BubbleGrid reference missing!");

            if (levelManager == null)
                Debug.LogError("[BubbleShooter] LevelManager reference missing!");

            if (shooterController == null)
                Debug.LogError("[BubbleShooter] ShooterController reference missing!");

            if (aimController == null)
                Debug.LogError("[BubbleShooter] AimController reference missing!");

            if (matchFinder == null)
                Debug.LogError("[BubbleShooter] BubbleMatchFinder reference missing!");

            if (floatingDetector == null)
                Debug.LogError("[BubbleShooter] FloatingBubbleDetector reference missing!");

            if (scoreManager == null)
                Debug.LogError("[BubbleShooter] ScoreManager reference missing!");

            if (gameplayUI == null)
                Debug.LogError("[BubbleShooter] GameplayUI reference missing!");
        }

        // ---------------------------------------------------------------
        // Setup
        // ---------------------------------------------------------------

        public void InitializeGameplay()
        {
            SetState(GameState.Loading);

            Debug.Log("[BubbleShooter] Gameplay initialized.");

            if (colorGenerator != null)
                colorGenerator.Initialize(bubbleGrid);

            if (matchFinder != null)
                matchFinder.Initialize(bubbleGrid);

            if (floatingDetector != null)
                floatingDetector.Initialize(bubbleGrid);

            if (levelManager != null)
                levelManager.Initialize(bubbleGrid, bubblePrefab);

            BindEventsOnce();

            StartLevel(false);
        }

        private void BindEventsOnce()
        {
            if (eventsBound)
                return;

            eventsBound = true;

            if (gameplayUI != null)
            {
                gameplayUI.OnPauseClicked += PauseGame;
                gameplayUI.OnResumeClicked += ResumeGame;
                gameplayUI.OnRetryClicked += RetryLevel;
                gameplayUI.OnHomeClicked += ReturnToHome;
                gameplayUI.OnNextLevelClicked += NextLevel;
            }

            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += (score) =>
                {
                    if (gameplayUI != null)
                        gameplayUI.UpdateScoreText(score);
                };
            }

            if (aimController != null)
                aimController.OnAimReleased += HandleAimReleased;
        }

        // ---------------------------------------------------------------
        // Gameplay bounds
        // ---------------------------------------------------------------

        private void GetPlayBounds(
            out float left,
            out float right,
            out float ceiling)
        {
            if (useGridBounds && bubbleGrid != null)
            {
                float diameter = bubbleGrid.BubbleDiameter;

                left = bubbleGrid.LeftWallX - diameter * 0.5f;
                right = bubbleGrid.RightWallX + diameter * 0.5f;
                ceiling = bubbleGrid.CeilingY + diameter * 0.25f;
            }
            else
            {
                left = leftWallX;
                right = rightWallX;
                ceiling = ceilingY;
            }
        }

        private void ApplyAimBounds()
        {
            if (aimController == null)
                return;

            GetPlayBounds(
                out float left,
                out float right,
                out float ceiling);

            aimController.SetBounds(left, right, ceiling);
        }

        // ---------------------------------------------------------------
        // Start / reset level
        // ---------------------------------------------------------------

        private void StartLevel(bool alreadyLoaded)
        {
            StopAllCoroutines();

            swapPressActive = false;
            Time.timeScale = 1f;

            SetState(GameState.Loading);

            if (gameplayUI != null)
                gameplayUI.HideAllPanels();

            if (scoreManager != null)
                scoreManager.ResetScore();

            if (aimController != null)
                aimController.HideAimLine();

            // Retry / Next ke waqt hawa mein udti bubble naye level mein na ja lage
            foreach (BubbleProjectile p in FindObjectsOfType<BubbleProjectile>())
            {
                if (p != null)
                    Destroy(p.gameObject);
            }

            if (!alreadyLoaded && levelManager != null)
                levelManager.LoadLevel();

            ApplyAimBounds();

            if (gameplayUI != null &&
                levelManager != null &&
                levelManager.CurrentLevelData != null)
            {
                gameplayUI.UpdateLevelText(
                    levelManager.CurrentLevelData.levelNumber);
            }

            if (shooterController != null &&
                levelManager != null &&
                levelManager.CurrentLevelData != null)
            {
                shooterController.SetupShooter(
                    levelManager.CurrentLevelData.availableColors);
            }

            RefreshShooterColors();

            missCount = 0;
            UpdateMissesUI();

            SetupLevelVisuals(true);

            SetState(GameState.Ready);

            firstShotTaken = false;

            if (gameplayUI != null)
            {
                gameplayUI.SetInstructionText(
                    "Drag or Click to Aim, Release to Shoot!", true);
                gameplayUI.ShowInstruction();
            }

            if (levelManager != null &&
                levelManager.CurrentLevelData != null)
            {
                Debug.Log(
                    $"[BubbleShooter] Level " +
                    $"{levelManager.CurrentLevelData.levelNumber} started.");
            }
        }

        // ---------------------------------------------------------------
        // Visuals (theme, danger line, intro)
        // ---------------------------------------------------------------

        private void SetupLevelVisuals(bool playIntro)
        {
            if (GameFX.Exists)
                GameFX.Instance.ClearAll();

            dangerLineY = ComputeDangerLineY();

            if (!enableEffects)
                return;

            int levelNumber =
                levelManager != null && levelManager.CurrentLevelData != null
                    ? levelManager.CurrentLevelData.levelNumber
                    : 1;

            backdrop = GameplayBackdrop.Ensure();
            backdrop.Apply(levelNumber, bubbleGrid, dangerLineY);
            UpdateDangerVisual();

            if (shooterController != null)
                ShooterVisuals.Attach(shooterController, aimController, bubbleGrid);

            if (playIntro && bubbleGrid != null)
                bubbleGrid.PlayIntroWave();

            if (gameplayUI != null)
                gameplayUI.ShowLevelBanner(levelNumber, backdrop.CurrentTheme.name);
        }

        /// <summary>
        /// Khatre ki lakeer: shooter se thora upar. Pehle dangerRow (28) grid ki
        /// aakhri row se bhi neeche tha, is liye bubbles shooter ke neeche tak chali jati thin.
        /// </summary>
        private float ComputeDangerLineY()
        {
            float d = bubbleGrid != null ? bubbleGrid.BubbleDiameter : 0.4f;

            float shooterY = -3.4f;
            if (shooterController != null && shooterController.CurrentBubbleAnchor != null)
                shooterY = shooterController.CurrentBubbleAnchor.position.y;

            float line = shooterY + d * dangerLineDiameters;

            // Choti / chauri screens par level shuru se hi lakeer ke paas ho sakta hai:
            // kam az kam 3 rows ki jagah do, taake level foran fail na ho.
            if (bubbleGrid != null)
            {
                float? lowest = bubbleGrid.GetLowestBubbleY();
                if (lowest.HasValue)
                {
                    float minGap = d * 3f;
                    if (lowest.Value - d * 0.5f - line < minGap)
                        line = lowest.Value - d * 0.5f - minGap;
                }
            }

            return line;
        }

        /// <summary>Koi bubble lakeer ko chhoo gayi?</summary>
        private bool BubblesCrossedDangerLine()
        {
            if (bubbleGrid == null) return false;
            float? lowest = bubbleGrid.GetLowestBubbleY();
            if (!lowest.HasValue) return false;
            return lowest.Value - bubbleGrid.BubbleDiameter * 0.5f <= dangerLineY;
        }

        private void UpdateDangerVisual()
        {
            if (backdrop == null || bubbleGrid == null) return;

            float? lowest = bubbleGrid.GetLowestBubbleY();
            if (!lowest.HasValue)
            {
                backdrop.SetDanger(0f);
                return;
            }

            float d = bubbleGrid.BubbleDiameter;
            float gap = lowest.Value - d * 0.5f - dangerLineY;
            // 4 rows door = 0, lakeer par = 1
            backdrop.SetDanger(1f - Mathf.Clamp01(gap / (d * 4f)));
        }

        // ---------------------------------------------------------------
        // Input / shooting
        // ---------------------------------------------------------------

        private void Update()
        {
            if (currentState != GameState.Ready &&
                currentState != GameState.Aiming)
            {
                swapPressActive = false;
            }

            if (currentState != GameState.Ready &&
                currentState != GameState.Aiming)
            {
                return;
            }

            if (aimController == null ||
                shooterController == null ||
                shooterController.CurrentBubble == null)
            {
                return;
            }

            if (HandleSwapInput())
                return;

            BubbleController currentBubble =
                shooterController.CurrentBubble;

            SpriteRenderer bubbleRenderer =
                currentBubble.GetComponentInChildren<SpriteRenderer>();

            if (bubbleRenderer != null)
                aimController.SetAimColor(bubbleRenderer.color);

            aimController.ProcessAimInput(
                currentBubble.transform.position,
                true);

            if (aimController.IsAiming &&
                currentState == GameState.Ready)
            {
                SetState(GameState.Aiming);
            }
        }

        private static void ReadPointer(
            out Vector2 position,
            out bool down,
            out bool up)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                position = touch.position;
                down = touch.phase == TouchPhase.Began;
                up = touch.phase == TouchPhase.Ended ||
                     touch.phase == TouchPhase.Canceled;

                return;
            }

            position = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            up = Input.GetMouseButtonUp(0);
        }

        private bool HandleSwapInput()
        {
            if (shooterController == null ||
                aimController == null)
            {
                return false;
            }

            if (shooterController.IsSwapping)
                return true;

            ReadPointer(
                out Vector2 position,
                out bool down,
                out bool up);

            if (down &&
                !aimController.IsAiming &&
                shooterController.IsOnSwapZone(position))
            {
                swapPressActive = true;
            }

            if (!swapPressActive)
                return false;

            if (up)
            {
                swapPressActive = false;

                if (shooterController.IsOnSwapZone(position))
                    shooterController.SwapBubbles();
            }

            return true;
        }

        private void HandleAimReleased(Vector2 direction)
        {
            if (currentState != GameState.Ready &&
                currentState != GameState.Aiming)
            {
                return;
            }

            SetState(GameState.Shooting);

            if (!firstShotTaken)
            {
                firstShotTaken = true;
                if (gameplayUI != null)
                    gameplayUI.HideInstruction();
            }

            Debug.Log(
                $"[BubbleShooter] Shot fired direction: {direction}");

            GetPlayBounds(
                out float left,
                out float right,
                out float ceiling);

            // This callback now matches:
            // Action<BubbleController, GridCoordinate, bool>
            bool shot = shooterController.Shoot(
                direction,
                left,
                right,
                ceiling,
                OnBubbleAttached);

            if (!shot)
                SetState(GameState.Ready);
        }

        // ---------------------------------------------------------------
        // Bubble attached callback
        // ---------------------------------------------------------------

        private void OnBubbleAttached(
            BubbleController attachedBubble,
            GridCoordinate coord,
            bool skipMatchProcessing)
        {
            Debug.Log(
                $"[BubbleShooter] Bubble attached at row {coord.Row}, " +
                $"column {coord.Column}. " +
                $"Skip match processing: {skipMatchProcessing}");

            // IMPORTANT:
            // First direct hit on a locked bubble removes its rope.
            // Do not run match detection or drop floating bubbles for
            // this particular shot.
            if (skipMatchProcessing)
            {
                RefreshShooterColors();
                SetState(GameState.Ready);
                return;
            }

            // Normal shot: run standard match and shot-resolution logic.
            SetState(GameState.Processing);

            StartCoroutine(
                ProcessGridMatchesRoutine(attachedBubble, coord));
        }

        // ---------------------------------------------------------------
        // Shot resolution
        // ---------------------------------------------------------------

        private IEnumerator ProcessGridMatchesRoutine(
            BubbleController attachedBubble,
            GridCoordinate coord)
        {
            yield return new WaitForSeconds(0.05f);

            int minMatch =
                levelManager != null &&
                levelManager.CurrentLevelData != null
                    ? levelManager.CurrentLevelData.minimumMatchCount
                    : 3;

            List<BubbleController> matches =
                matchFinder != null
                    ? matchFinder.FindMatches(attachedBubble, minMatch)
                    : new List<BubbleController>();

            if (matches.Count >= minMatch)
            {
                Debug.Log(
                    $"[BubbleShooter] Match found: {matches.Count} bubbles.");

                if (resetMissesOnMatch)
                {
                    missCount = 0;
                    UpdateMissesUI();
                }

                // 1. Matched bubbles: unlocked pop hongi,
                //    locked (zanjeer wali) sirf unlock hongi.
                int poppedCount = 0;
                int unlockedCount = 0;
                Vector3 popCenter = Vector3.zero;
                Color popColor = attachedBubble != null
                    ? BubbleColorUtility.GetColorValue(attachedBubble.Color)
                    : Color.white;

                foreach (BubbleController bubble in matches)
                {
                    if (bubble == null)
                        continue;

                    // Locked bubble: group mein hone par sirf rope khulti hai,
                    // bubble grid mein rehti hai (BubbleController ka apna lock).
                    if (bubble.IsLocked)
                    {
                        bubble.ApplyHit();
                        unlockedCount++;
                        continue;
                    }

                    bubbleGrid.RemoveBubble(
                        bubble.GridPosition.Row,
                        bubble.GridPosition.Column);

                    popCenter += bubble.transform.position;
                    bubble.Pop();
                    poppedCount++;
                }

                if (poppedCount > 0)
                    popCenter /= poppedCount;

                Debug.Log(
                    $"[BubbleShooter] Match result: {poppedCount} popped, " +
                    $"{unlockedCount} locked bubble(s) only unlocked.");

                int scoreBefore = scoreManager != null ? scoreManager.CurrentScore : 0;

                if (scoreManager != null && poppedCount > 0)
                    scoreManager.AddMatchScore(poppedCount);

                int matchPoints = scoreManager != null ? scoreManager.CurrentScore - scoreBefore : 0;

                // 2. Drop bubbles that are no longer connected to the ceiling.
                Vector3 dropCenter = Vector3.zero;
                List<GridCoordinate> floatingNow = bubbleGrid.GetFloatingCoordinates();
                foreach (GridCoordinate fc in floatingNow)
                    dropCenter += bubbleGrid.GridToWorldPosition(fc);
                if (floatingNow.Count > 0)
                    dropCenter /= floatingNow.Count;

                int fallenCount = bubbleGrid.DropFloatingBubbles();
                int dropPoints = 0;

                if (fallenCount > 0)
                {
                    Debug.Log(
                        $"[BubbleShooter] Floating bubbles dropped: " +
                        $"{fallenCount}.");

                    // FIX: girne wali bubbles ka apna score (pehle combo counter
                    // bhi barhta tha, is liye score ajeeb tarah se uchhalta tha)
                    int before = scoreManager != null ? scoreManager.CurrentScore : 0;
                    if (scoreManager != null)
                        scoreManager.AddFloatingScore(fallenCount);
                    dropPoints = scoreManager != null ? scoreManager.CurrentScore - before : 0;
                }

                PlayMatchFeedback(poppedCount, fallenCount, matchPoints, dropPoints,
                    popCenter, dropCenter, popColor);

                yield return new WaitForSeconds(0.2f);

                // 3. Clear the final one or two bubbles if no rows remain.
                bool noPendingRows =
                    levelManager == null ||
                    levelManager.PendingRowsRemaining <= 0;

                List<BubbleController> remainingBubbles =
                    bubbleGrid.GetAllBubbles();

                if (noPendingRows &&
                    remainingBubbles.Count <= 2 &&
                    remainingBubbles.Count > 0)
                {
                    foreach (BubbleController bubble in remainingBubbles)
                    {
                        if (bubble == null)
                            continue;

                        GridCoordinate position =
                            bubble.GetGridPosition();

                        bubbleGrid.RemoveBubble(
                            position.Row,
                            position.Column);

                        Vector2 velocity = new Vector2(
                            Random.Range(-2f, 2f),
                            Random.Range(1f, 3f));

                        bubble.Fall(velocity);
                    }

                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                // Shot missed: remove the attached bubble if configured.
                if (removeMissedBubble && attachedBubble != null)
                {
                    bubbleGrid.RemoveBubble(
                        coord.Row,
                        coord.Column);

                    attachedBubble.Pop(false);
                }

                missCount++;
                UpdateMissesUI();

                if (scoreManager != null)
                    scoreManager.ResetCombo();

                int maxMisses = GetMaxMisses();

                Debug.Log(
                    $"[BubbleShooter] Missed shot {missCount}/" +
                    $"{(maxMisses > 0 ? maxMisses.ToString() : "unlimited")}.");

                if (maxMisses > 0 && missCount >= maxMisses)
                {
                    SetState(GameState.Lose);

                    Debug.Log(
                        "[BubbleShooter] Level Failed (miss limit reached).");

                    yield return new WaitForSeconds(0.9f);

                    if (gameplayUI != null)
                    {
                        gameplayUI.ShowLosePanel(
                            scoreManager != null
                                ? scoreManager.CurrentScore
                                : 0);
                    }

                    yield break;
                }
            }

            // 4. Check whether the level is complete.
            if (levelManager != null &&
                levelManager.CheckWinCondition())
            {
                SetState(GameState.Win);

                Debug.Log("[BubbleShooter] Level Complete.");

                // Main Menu ke coin counter ke liye inaam
                CoinWallet.Add(CoinWallet.CoinsPerLevelWin);

                if (enableEffects)
                {
                    if (GameFX.Exists) GameFX.Instance.Shake(0.12f, 0.4f);
                    if (gameplayUI != null)
                        gameplayUI.ShowPraise("CLEARED!", new Color(1f, 0.9f, 0.3f));
                    yield return new WaitForSeconds(0.9f);
                }

                if (gameplayUI != null)
                {
                    gameplayUI.ShowWinPanel(
                        scoreManager != null
                            ? scoreManager.CurrentScore
                            : 0);
                }

                yield break;
            }

            // 5. Add a new row when required.
            if (levelManager != null)
            {
                bool forceRow =
                    levelManager.PendingRowsRemaining > 0 &&
                    bubbleGrid.GetAllBubbles().Count <= 2;

                bool rowOk = true;

                yield return StartCoroutine(
                    levelManager.ResolveShotAndMaybeAddRow(
                        ok => rowOk = ok,
                        forceRow));

                if (!rowOk)
                {
                    SetState(GameState.Lose);

                    Debug.Log(
                        "[BubbleShooter] Level Failed " +
                        "(bubbles reached the bottom).");

                    if (gameplayUI != null)
                    {
                        gameplayUI.ShowLosePanel(
                            scoreManager != null
                                ? scoreManager.CurrentScore
                                : 0);
                    }

                    yield break;
                }
            }

            UpdateDangerVisual();

            // 6. Check the danger row (level ka dangerRow YA shooter ke upar wali lakeer).
            if ((levelManager != null &&
                 levelManager.CheckLoseCondition()) ||
                BubblesCrossedDangerLine())
            {
                if (enableEffects && GameFX.Exists)
                    GameFX.Instance.Shake(0.15f, 0.35f);
                SetState(GameState.Lose);

                Debug.Log("[BubbleShooter] Level Failed.");

                if (gameplayUI != null)
                {
                    gameplayUI.ShowLosePanel(
                        scoreManager != null
                            ? scoreManager.CurrentScore
                            : 0);
                }

                yield break;
            }

            // 7. Refresh the shooter's colors.
            RefreshShooterColors();

            // Ready for the next shot.
            SetState(GameState.Ready);
        }

        /// <summary>Score popups, "GREAT!" jaisa text aur camera ka halka jhatka.</summary>
        private void PlayMatchFeedback(
            int popped, int fallen, int matchPoints, int dropPoints,
            Vector3 popCenter, Vector3 dropCenter, Color popColor)
        {
            if (!enableEffects)
                return;

            GameFX fx = GameFX.Instance;
            if (fx == null)
                return;

            if (popped > 0 && matchPoints > 0)
                fx.FloatingText(popCenter, "+" + matchPoints, Color.Lerp(popColor, Color.white, 0.35f), 1f);

            if (fallen > 0 && dropPoints > 0)
                fx.FloatingText(dropCenter + Vector3.up * 0.2f, "+" + dropPoints, new Color(1f, 0.92f, 0.35f), 1.2f, 1.1f);

            int total = popped + fallen;
            int combo = scoreManager != null ? scoreManager.ComboCount : 0;

            if (total >= 6)
                fx.Shake(Mathf.Clamp(0.03f + total * 0.006f, 0.04f, 0.16f), 0.3f);

            if (gameplayUI == null)
                return;

            if (total >= 14)
                gameplayUI.ShowPraise("INCREDIBLE!", new Color(1f, 0.45f, 0.9f));
            else if (total >= 9)
                gameplayUI.ShowPraise("AMAZING!", new Color(0.4f, 0.95f, 1f));
            else if (total >= 6)
                gameplayUI.ShowPraise("GREAT!", new Color(0.5f, 1f, 0.45f));
            else if (combo >= 3)
                gameplayUI.ShowPraise("COMBO x" + combo + "!", new Color(1f, 0.85f, 0.3f));
        }

        private int GetMaxMisses()
        {
            if (levelManager == null ||
                levelManager.CurrentLevelData == null)
            {
                return fallbackMaxMisses;
            }

            int max = levelManager.CurrentLevelData.maximumMissedShots;

            return max > 0 ? max : fallbackMaxMisses;
        }

        private void UpdateMissesUI()
        {
            int max = GetMaxMisses();

            Debug.Log(
                $"[MissDebug] Level " +
                $"{(levelManager != null && levelManager.CurrentLevelData != null ? levelManager.CurrentLevelData.levelNumber : -1)}: " +
                $"misses {missCount}/{max}, " +
                $"indicator assigned: {missIndicator != null}, " +
                $"indicator active: {(missIndicator != null && missIndicator.gameObject.activeInHierarchy)}");

            if (gameplayUI != null)
                gameplayUI.UpdateMissesText(missCount, max);

            if (missIndicator != null)
                missIndicator.SetMisses(missCount, max);
        }

        private void RefreshShooterColors()
        {
            if (shooterController != null)
                shooterController.EnsureColorsInGrid();
        }
        private void ShowCPAd()
        {
            if (CheckCpAd.instance != null)
            {
                Debug.Log("[CPADS] Requesting CP ad.");

                CheckCpAd.instance.CallAd();
            }
            else
            {
                Debug.LogWarning(
                    "[CPADS] CheckCpAd instance is NULL.");
            }
        }

        // ---------------------------------------------------------------
        // State / buttons
        // ---------------------------------------------------------------

        public void SetState(GameState newState)
        {
            currentState = newState;
        }

        public void PauseGame()
        {
            if (currentState == GameState.Win ||
                currentState == GameState.Lose ||
                currentState == GameState.Paused)
            {
                return;
            }

            Debug.Log("[CPADS] Pause clicked.");

            ShowCPAd();

            stateBeforePause = currentState;
            SetState(GameState.Paused);

            if (gameplayUI != null)
                gameplayUI.ShowPausePanel(true);

            if (aimController != null)
                aimController.HideAimLine();

            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused)
                return;

            Time.timeScale = 1f;

            if (gameplayUI != null)
                gameplayUI.ShowPausePanel(false);

            // Udti bubble / processing ke dauran pause hua tha to wahi state wapas,
            // warna coroutine ke khatam hone se pehle doosra shot chal jata tha.
            GameState restore = stateBeforePause;
            if (restore == GameState.Aiming || restore == GameState.Paused)
                restore = GameState.Ready;

            SetState(restore);
        }

        public void RetryLevel()
        {
            Debug.Log("[BubbleShooter] Retry clicked.");
            StartLevel(false);
        }

        public void ReturnToHome()
        {
            Debug.Log("[CPADS] Home clicked.");

            ShowCPAd();

            Time.timeScale = 1f;

            SetState(GameState.Loading);
            SceneFader.LoadScene("MainMenu");
        }

        public void NextLevel()
        {
            Debug.Log("[CPADS] Next Level clicked.");

            ShowCPAd();

            Time.timeScale = 1f;

            if (levelManager != null &&
                levelManager.LoadNextLevel())
            {
                StartLevel(true);
            }
            else
            {
                Debug.Log(
                    "[BubbleShooter] All levels complete. Restarting Level 1.");

                if (levelManager != null)
                    levelManager.LoadFirstLevel();

                StartLevel(true);
            }
        }
    }
}