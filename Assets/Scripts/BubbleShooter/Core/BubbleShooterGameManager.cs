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
        [Tooltip("ON: walls aur ceiling BubbleGrid (diameter/columns) se khud nikalte hain. Bubble size badalne par kuch manually nahi badalna.")]
        [SerializeField] private bool useGridBounds = true;
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float ceilingY = 3.8f;

        [Header("Miss Rule")]
        [Tooltip("ON: koi shot match kare to miss counter 0 ho jata hai (lagataar misses). " +
                 "OFF: poore level mein total misses gine jate hain.")]
        [SerializeField] private bool resetMissesOnMatch = false;
        [Tooltip("ON: jo shot miss ho (match na bane) wo bubble grid se chipakne ke bajaye ghaib ho jati hai.")]
        [SerializeField] private bool removeMissedBubble = true;

        private int missCount = 0;
        private bool eventsBound = false;
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
            if (bubbleGrid == null) Debug.LogError("[BubbleShooter] BubbleGrid reference missing on GameManager!");
            if (levelManager == null) Debug.LogError("[BubbleShooter] LevelManager reference missing on GameManager!");
            if (shooterController == null) Debug.LogError("[BubbleShooter] ShooterController reference missing on GameManager!");
            if (aimController == null) Debug.LogError("[BubbleShooter] AimController reference missing on GameManager!");
            if (matchFinder == null) Debug.LogError("[BubbleShooter] BubbleMatchFinder reference missing on GameManager!");
            if (floatingDetector == null) Debug.LogError("[BubbleShooter] FloatingBubbleDetector reference missing on GameManager!");
            if (scoreManager == null) Debug.LogError("[BubbleShooter] ScoreManager reference missing on GameManager!");
            if (gameplayUI == null) Debug.LogError("[BubbleShooter] GameplayUI reference missing on GameManager!");
        }

        // ---------------------------------------------------------------
        // Setup
        // ---------------------------------------------------------------

        /// <summary>Game shuru hone par ek baar chalta hai: subsystems init + events bind + level 1.</summary>
        public void InitializeGameplay()
        {
            SetState(GameState.Loading);
            Debug.Log("[BubbleShooter] Gameplay initialized.");

            // Subsystem cross-initialization
            if (colorGenerator != null) colorGenerator.Initialize(bubbleGrid);
            if (matchFinder != null) matchFinder.Initialize(bubbleGrid);
            if (floatingDetector != null) floatingDetector.Initialize(bubbleGrid);
            if (levelManager != null) levelManager.Initialize(bubbleGrid, bubblePrefab);

            BindEventsOnce();

            StartLevel(false);
        }

        /// <summary>Events sirf ek baar bind hote hain (Retry/Next par dobara nahi).</summary>
        private void BindEventsOnce()
        {
            if (eventsBound) return;
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
                scoreManager.OnScoreChanged += (score) => {
                    if (gameplayUI != null) gameplayUI.UpdateScoreText(score);
                };
            }

            if (aimController != null)
            {
                aimController.OnAimReleased += HandleAimReleased;
            }
        }

        /// <summary>
        /// Shooter/aim ke liye playfield ki hadein. useGridBounds ON ho to grid se nikalti hain,
        /// is liye bubble ka size ya columns badalne par walls khud sahi ho jate hain.
        /// </summary>
        private void GetPlayBounds(out float left, out float right, out float ceiling)
        {
            if (useGridBounds && bubbleGrid != null)
            {
                float d = bubbleGrid.BubbleDiameter;
                left = bubbleGrid.LeftWallX - d * 0.5f;      // wall ka edge
                right = bubbleGrid.RightWallX + d * 0.5f;
                ceiling = bubbleGrid.CeilingY + d * 0.25f;
            }
            else
            {
                left = leftWallX;
                right = rightWallX;
                ceiling = ceilingY;
            }
        }

        /// <summary>
        /// Current level ko shuru karta hai (UI reset, shooter setup, state Ready).
        /// alreadyLoaded = true ho to LevelManager.LoadLevel() dobara nahi chalta
        /// (jaise LoadNextLevel() ne level pehle hi load kar diya ho).
        /// </summary>
        private void StartLevel(bool alreadyLoaded)
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            SetState(GameState.Loading);

            if (gameplayUI != null) gameplayUI.HideAllPanels();
            if (scoreManager != null) scoreManager.ResetScore();

            if (aimController != null)
            {
                GetPlayBounds(out float l, out float r, out float c);
                aimController.SetBounds(l, r, c);
                aimController.HideAimLine();
            }

            // LevelManager.LoadLevel() grid khud clear karta hai
            if (!alreadyLoaded && levelManager != null)
            {
                levelManager.LoadLevel();
            }

            if (gameplayUI != null && levelManager != null && levelManager.CurrentLevelData != null)
            {
                gameplayUI.UpdateLevelText(levelManager.CurrentLevelData.levelNumber);
            }

            // Setup Shooter
            if (shooterController != null && levelManager != null && levelManager.CurrentLevelData != null)
            {
                shooterController.SetupShooter(levelManager.CurrentLevelData.availableColors);
            }

            // Shooter ki bubble ka color grid ke colors mein se ho
            RefreshShooterColors();

            missCount = 0;
            UpdateMissesUI();
            SetState(GameState.Ready);

            if (gameplayUI != null)
            {
                gameplayUI.SetInstructionText("Drag or Click to Aim, Release to Shoot!", true);
            }

            if (levelManager != null && levelManager.CurrentLevelData != null)
                Debug.Log($"[BubbleShooter] Level {levelManager.CurrentLevelData.levelNumber} started.");
        }

        // ---------------------------------------------------------------
        // Input / shooting
        // ---------------------------------------------------------------

        private void Update()
        {
            if (currentState == GameState.Ready || currentState == GameState.Aiming)
            {
                if (aimController != null && shooterController != null && shooterController.CurrentBubble != null)
                {
                    aimController.ProcessAimInput(shooterController.CurrentBubble.transform.position, true);
                    if (aimController.IsAiming && currentState == GameState.Ready)
                    {
                        SetState(GameState.Aiming);
                    }
                }
            }
        }

        private void HandleAimReleased(Vector2 direction)
        {
            if (currentState != GameState.Ready && currentState != GameState.Aiming)
                return;

            SetState(GameState.Shooting);
            if (gameplayUI != null) gameplayUI.SetInstructionText("", false);

            Debug.Log($"[BubbleShooter] Shot fired direction: {direction}");

            GetPlayBounds(out float left, out float right, out float ceiling);
            bool shot = shooterController.Shoot(direction, left, right, ceiling, OnBubbleAttached);
            if (!shot)
            {
                SetState(GameState.Ready);
            }
        }

        private void OnBubbleAttached(BubbleController attachedBubble, GridCoordinate coord)
        {
            Debug.Log($"[BubbleShooter] Bubble attached at row {coord.Row} column {coord.Column}.");
            SetState(GameState.Processing);
            StartCoroutine(ProcessGridMatchesRoutine(attachedBubble, coord));
        }

        // ---------------------------------------------------------------
        // Shot resolution
        // ---------------------------------------------------------------

        private IEnumerator ProcessGridMatchesRoutine(BubbleController attachedBubble, GridCoordinate coord)
        {
            yield return new WaitForSeconds(0.05f);

            int minMatch = levelManager != null && levelManager.CurrentLevelData != null ? levelManager.CurrentLevelData.minimumMatchCount : 3;
            List<BubbleController> matches = matchFinder != null ? matchFinder.FindMatches(attachedBubble, minMatch) : new List<BubbleController>();

            if (matches.Count >= minMatch)
            {
                Debug.Log($"[BubbleShooter] Match found: {matches.Count} bubbles.");
                if (resetMissesOnMatch)
                {
                    missCount = 0;
                    UpdateMissesUI();
                }

                // 1. Pop matched bubbles (pehle grid se hatao, phir animation)
                int poppedCount = matches.Count;
                foreach (var b in matches)
                {
                    if (b != null)
                    {
                        bubbleGrid.RemoveBubble(b.GridPosition.Row, b.GridPosition.Column);
                        b.Pop();
                    }
                }

                if (scoreManager != null)
                {
                    scoreManager.AddMatchScore(poppedCount);
                }

                // 2. Floating bubbles: poore grid ka check.
                //    Jo bubbles top se judi nahi (chahe kisi bhi color ki hon), sab gir jati hain.
                int fallenCount = bubbleGrid.DropFloatingBubbles();
                if (fallenCount > 0)
                {
                    Debug.Log($"[BubbleShooter] Floating bubbles dropped: {fallenCount}.");

                    if (scoreManager != null)
                    {
                        // Agar ScoreManager mein floating ka alag method ho to yahan wo use karo
                        scoreManager.AddMatchScore(fallenCount);
                    }
                }

                yield return new WaitForSeconds(0.2f);

                // 3. Aakhri 1-2 bubbles bachi hon to unhe bhi gira do
                //    (sirf tab jab nayi rows aani baaqi na hon)
                bool noPendingRows = levelManager == null || levelManager.PendingRowsRemaining <= 0;
                List<BubbleController> remainingBubbles = bubbleGrid.GetAllBubbles();

                if (noPendingRows && remainingBubbles.Count <= 2 && remainingBubbles.Count > 0)
                {
                    foreach (BubbleController bubble in remainingBubbles)
                    {
                        GridCoordinate pos = bubble.GetGridPosition();
                        bubbleGrid.RemoveBubble(pos.Row, pos.Column);

                        Vector2 velocity = new Vector2(
                            UnityEngine.Random.Range(-2f, 2f),
                            UnityEngine.Random.Range(1f, 3f));
                        bubble.Fall(velocity);
                    }

                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                // Shot missed (koi match nahi bana)

                // Missed bubble grid se hata do aur ghaib kar do
                if (removeMissedBubble && attachedBubble != null)
                {
                    bubbleGrid.RemoveBubble(coord.Row, coord.Column);
                    attachedBubble.Pop();
                }

                missCount++;
                UpdateMissesUI();

                if (scoreManager != null)
                {
                    scoreManager.ResetCombo();
                }

                int maxMisses = GetMaxMisses();
                Debug.Log($"[BubbleShooter] Missed shot {missCount}/{(maxMisses > 0 ? maxMisses.ToString() : "unlimited")}.");

                // Miss limit poori ho gayi => Game Over
                if (maxMisses > 0 && missCount >= maxMisses)
                {
                    SetState(GameState.Lose);
                    Debug.Log("[BubbleShooter] Level Failed (miss limit reached).");
                    if (gameplayUI != null) gameplayUI.ShowLosePanel(scoreManager != null ? scoreManager.CurrentScore : 0);
                    yield break;
                }
            }

            // 4. Win check (LevelManager.CheckWinCondition ab pending rows bhi dekhta hai)
            if (levelManager != null && levelManager.CheckWinCondition())
            {
                SetState(GameState.Win);
                Debug.Log("[BubbleShooter] Level Complete.");
                if (gameplayUI != null) gameplayUI.ShowWinPanel(scoreManager != null ? scoreManager.CurrentScore : 0);
                yield break;
            }

            // 5. Nayi row upar se girana (har shotsPerNewRow shots ke baad)
            if (levelManager != null)
            {
                // Grid lagbhag khali ho aur rows baaqi hon to row foran aa jaye
                bool forceRow = levelManager.PendingRowsRemaining > 0 && bubbleGrid.GetAllBubbles().Count <= 2;

                bool rowOk = true;
                yield return StartCoroutine(
                    levelManager.ResolveShotAndMaybeAddRow(ok => rowOk = ok, forceRow));

                if (!rowOk)
                {
                    SetState(GameState.Lose);
                    Debug.Log("[BubbleShooter] Level Failed (bubbles reached the bottom).");
                    if (gameplayUI != null) gameplayUI.ShowLosePanel(scoreManager != null ? scoreManager.CurrentScore : 0);
                    yield break;
                }
            }

            // 6. Lose check (danger row)
            if (levelManager != null && levelManager.CheckLoseCondition())
            {
                SetState(GameState.Lose);
                Debug.Log("[BubbleShooter] Level Failed.");
                if (gameplayUI != null) gameplayUI.ShowLosePanel(scoreManager != null ? scoreManager.CurrentScore : 0);
                yield break;
            }

            // 7. Shooter ki bubble ko grid ke maujooda colors mein se rakho
            RefreshShooterColors();

            // Ready for next shot
            SetState(GameState.Ready);
        }

        /// <summary>Is level mein kitne misses ki ijazat hai (0 = koi limit nahi).</summary>
        private int GetMaxMisses()
        {
            if (levelManager == null || levelManager.CurrentLevelData == null) return 0;
            return levelManager.CurrentLevelData.maximumMissedShots;
        }

        private void UpdateMissesUI()
        {
            int max = GetMaxMisses();

            if (gameplayUI != null) gameplayUI.UpdateMissesText(missCount, max);
            if (missIndicator != null) missIndicator.SetMisses(missCount, max);
        }

        /// <summary>
        /// Pops ke baad kuch colors grid se khatam ho sakte hain:
        /// shooter ki current AUR next bubble ko grid ke maujooda colors mein se rakho.
        /// (Size ShooterController khud grid ke diameter ke mutabiq rakhta hai.)
        /// </summary>
        private void RefreshShooterColors()
        {
            if (shooterController == null) return;
            shooterController.EnsureColorsInGrid();
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
            if (currentState == GameState.Win || currentState == GameState.Lose) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;
            if (gameplayUI != null) gameplayUI.ShowPausePanel(true);
            if (aimController != null) aimController.HideAimLine();
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            if (gameplayUI != null) gameplayUI.ShowPausePanel(false);
            SetState(GameState.Ready);
        }

        /// <summary>Isi level ko dobara shuru karo.</summary>
        public void RetryLevel()
        {
            Debug.Log("[BubbleShooter] Retry clicked.");
            StartLevel(false);   // LevelManager.LoadLevel() current level dobara load karta hai
        }

        public void ReturnToHome()
        {
            Time.timeScale = 1f;
            Debug.Log("[BubbleShooter] Returning to MainMenu.");
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>Agle level par jao. Aakhri level ke baad Level 1 par wapas.</summary>
        public void NextLevel()
        {
            Debug.Log("[BubbleShooter] Next Level clicked.");

            if (levelManager != null && levelManager.LoadNextLevel())
            {
                StartLevel(true);   // LoadNextLevel ne level pehle hi load kar diya
            }
            else
            {
                Debug.Log("[BubbleShooter] All levels complete. Restarting from Level 1.");
                if (levelManager != null) levelManager.LoadFirstLevel();
                StartLevel(true);
            }
        }
    }
}