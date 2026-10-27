using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace BubbleShooter.UI
{
    public class GameplayUI : MonoBehaviour
    {
        [Header("Top Bar UI")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button pauseButton;
        [Tooltip("Optional: 'MISSES: 2/5' dikhane ke liye")]
        [SerializeField] private TextMeshProUGUI missesText;

        [Header("Instruction UI")]
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TextMeshProUGUI winScoreText;
        [SerializeField] private Button winNextButton;
        [SerializeField] private Button winRetryButton;
        [SerializeField] private Button winHomeButton;

        [SerializeField] private GameObject losePanel;
        [SerializeField] private TextMeshProUGUI loseScoreText;
        [SerializeField] private Button loseRetryButton;
        [SerializeField] private Button loseHomeButton;

        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseHomeButton;

        public event Action OnPauseClicked;
        public event Action OnResumeClicked;
        public event Action OnRetryClicked;
        public event Action OnHomeClicked;
        public event Action OnNextLevelClicked;

        private void Awake()
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.enabled = true;
            }
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => OnPauseClicked?.Invoke());

            if (winNextButton != null)
                winNextButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
            if (winRetryButton != null)
                winRetryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (winHomeButton != null)
                winHomeButton.onClick.AddListener(() => OnHomeClicked?.Invoke());

            if (loseRetryButton != null)
                loseRetryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (loseHomeButton != null)
                loseHomeButton.onClick.AddListener(() => OnHomeClicked?.Invoke());

            if (pauseResumeButton != null)
                pauseResumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
            if (pauseRestartButton != null)
                pauseRestartButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (pauseHomeButton != null)
                pauseHomeButton.onClick.AddListener(() => OnHomeClicked?.Invoke());
      

            WarnAboutMissingReferences();
        }

        /// <summary>
        /// Agar koi button Inspector mein assign nahi hai to uska listener lagta hi nahi
        /// aur click par kuch nahi hota. Ye warnings wahi pakadti hain.
        /// </summary>
        private void WarnAboutMissingReferences()
        {
            Warn(pauseButton, "Pause Button");
            Warn(winNextButton, "Win Next Button");
            Warn(winRetryButton, "Win Retry Button");
            Warn(winHomeButton, "Win Home Button");
            Warn(loseRetryButton, "Lose Retry Button");
            Warn(loseHomeButton, "Lose Home Button");
            Warn(pauseResumeButton, "Pause Resume Button");
            Warn(pauseRestartButton, "Pause Restart Button");
            Warn(pauseHomeButton, "Pause Home Button");

            if (FindObjectOfType<EventSystem>() == null)
            {
                Debug.LogError("[GameplayUI] Scene mein EventSystem nahi hai! " +
                               "GameObject > UI > Event System banao, warna koi button click nahi hoga.");
            }
        }

        private void Warn(Button button, string label)
        {
            if (button == null)
                Debug.LogWarning($"[GameplayUI] '{label}' Inspector mein assign nahi hai (is button par click kuch nahi karega).");
        }

        public void UpdateLevelText(int level)
        {
            if (levelText != null)
                levelText.text = "LEVEL " + level;
        }

        public void UpdateScoreText(int score)
        {
            if (scoreText != null)
                scoreText.text = "SCORE: " + score;
        }

        /// <summary>maxMisses 0 ya kam ho to text chhup jata hai (koi limit nahi).</summary>
        public void UpdateMissesText(int used, int maxMisses)
        {
            if (missesText == null) return;

            if (maxMisses <= 0)
            {
                missesText.gameObject.SetActive(false);
                return;
            }

            missesText.gameObject.SetActive(true);
            missesText.text = "MISSES: " + used + "/" + maxMisses;
        }

        public void SetInstructionText(string text, bool visible = true)
        {
            if (instructionText != null)
            {
                instructionText.text = text;

                // Instruction ko hamesha visible rakho.
                instructionText.gameObject.SetActive(true);
                instructionText.enabled = true;
            }
        }

        public void HideAllPanels()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void ShowWinPanel(int finalScore)
        {
            HideAllPanels();
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                if (winScoreText != null)
                    winScoreText.text = "FINAL SCORE: " + finalScore;
            }
        }

        public void ShowLosePanel(int finalScore)
        {
            HideAllPanels();
            if (losePanel != null)
            {
                losePanel.SetActive(true);
                if (loseScoreText != null)
                    loseScoreText.text = "FINAL SCORE: " + finalScore;
            }
        }

        public void ShowPausePanel(bool show)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(show);
            }
        }
    }
}