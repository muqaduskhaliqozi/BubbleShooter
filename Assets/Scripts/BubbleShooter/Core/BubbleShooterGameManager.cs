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

        private int missCount = 0;
        private bool eventsBound = false;
        private bool swapPressActive;

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

            SetState(GameState.Ready);

            if (gameplayUI != null)
            {
                gameplayUI.SetInstructionText(
                    "Drag or Click to Aim, Release to Shoot!", true);
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

            if (gameplayUI != null)
            {
                gameplayUI.SetInstructionText(
                    "Drag or Click to Aim, Release to Shoot!", true);
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

                    bubble.Pop();
                    poppedCount++;
                }

                Debug.Log(
                    $"[BubbleShooter] Match result: {poppedCount} popped, " +
                    $"{unlockedCount} locked bubble(s) only unlocked.");

                if (scoreManager != null && poppedCount > 0)
                    scoreManager.AddMatchScore(poppedCount);

                // 2. Drop bubbles that are no longer connected to the ceiling.
                int fallenCount = bubbleGrid.DropFloatingBubbles();

                if (fallenCount > 0)
                {
                    Debug.Log(
                        $"[BubbleShooter] Floating bubbles dropped: " +
                        $"{fallenCount}.");

                    if (scoreManager != null)
                        scoreManager.AddMatchScore(fallenCount);
                }

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

                    attachedBubble.Pop();
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

            // 6. Check the danger row.
            if (levelManager != null &&
                levelManager.CheckLoseCondition())
            {
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

            SetState(GameState.Paused);

            if (gameplayUI != null)
                gameplayUI.ShowPausePanel(true);

            if (aimController != null)
                aimController.HideAimLine();

            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;

            if (gameplayUI != null)
                gameplayUI.ShowPausePanel(false);

            SetState(GameState.Ready);
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

            SceneManager.LoadScene("MainMenu");
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