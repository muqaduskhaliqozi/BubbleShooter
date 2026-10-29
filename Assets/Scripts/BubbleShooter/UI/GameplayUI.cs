using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BubbleShooter.FX;

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

        [Header("Look (runtime polish)")]
        [Tooltip("Panels ke peeche andhera parda (dim) ka rang.")]
        [SerializeField] private Color panelDimColor = new Color(0.02f, 0.04f, 0.14f, 0.8f);
        [SerializeField] private bool animatePanels = true;

        public event Action OnPauseClicked;
        public event Action OnResumeClicked;
        public event Action OnRetryClicked;
        public event Action OnHomeClicked;
        public event Action OnNextLevelClicked;

        private RectTransform canvasRect;
        private int shownScore;
        private int targetScore;
        private Coroutine scoreRoutine;
        private Coroutine instructionRoutine;
        private Coroutine bannerRoutine;
        private Coroutine praiseRoutine;
        private Coroutine panelRoutine;

        private RectTransform bannerRoot;
        private TextMeshProUGUI bannerTitle;
        private TextMeshProUGUI bannerSubtitle;
        private Image bannerStripe;

        private TextMeshProUGUI praiseText;
        private RectTransform winSunburst;

        private readonly System.Collections.Generic.Dictionary<Image, float> dimAlpha =
            new System.Collections.Generic.Dictionary<Image, float>();

        private float instructionBaseAlpha = 1f;
        private bool instructionHidden;

        private void Awake()
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.enabled = true;
                instructionBaseAlpha = instructionText.color.a;
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
            DecorateUI();
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

        // =====================================================================
        // Runtime polish (scene mein kuch badle baghair)
        // =====================================================================

        private void DecorateUI()
        {
            canvasRect = UIBuild.RootCanvasRect(transform);
            if (canvasRect == null) canvasRect = transform as RectTransform;

            // Har button ko dabane par squish / bounce
            foreach (Button b in GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<UIButtonJuice>() == null)
                    b.gameObject.AddComponent<UIButtonJuice>();
            }

            // Next / Resume button halka sa dhadakte hain (aage barhne ka ishara)
            AddPulse(winNextButton, 0.05f);
            AddPulse(pauseResumeButton, 0.04f);

            // Panels ke peeche ka grey parda -> gehra neela
            RestyleDim(winPanel);
            RestyleDim(losePanel);
            RestyleDim(pausePanel);

            // Win panel ke peeche ghoomti roshni
            if (winPanel != null)
            {
                Image burst = UIBuild.Image("Sunburst", winPanel.transform, FxSprites.Sunburst,
                    new Color(1f, 0.85f, 0.35f, 0.45f));
                winSunburst = burst.rectTransform;
                winSunburst.anchorMin = winSunburst.anchorMax = new Vector2(0.5f, 0.5f);
                winSunburst.sizeDelta = new Vector2(1500f, 1500f);
                winSunburst.anchoredPosition = new Vector2(0f, 220f);
                winSunburst.SetSiblingIndex(0);
                burst.gameObject.AddComponent<UISpin>().degreesPerSecond = 14f;
            }

            BuildBanner();
            BuildPraise();
        }

        private static void AddPulse(Button b, float amount)
        {
            if (b == null) return;
            UIPulse p = b.GetComponent<UIPulse>();
            if (p == null) p = b.gameObject.AddComponent<UIPulse>();
            p.amount = amount;
        }

        private void RestyleDim(GameObject panelRoot)
        {
            if (panelRoot == null) return;
            Image img = panelRoot.GetComponent<Image>();
            if (img == null) return;
            if (img.sprite == null)
                img.color = panelDimColor;
            dimAlpha[img] = img.color.a;
        }

        private void BuildBanner()
        {
            bannerRoot = UIBuild.Rect("LevelBanner", transform);
            bannerRoot.anchorMin = new Vector2(0f, 0.5f);
            bannerRoot.anchorMax = new Vector2(1f, 0.5f);
            bannerRoot.sizeDelta = new Vector2(0f, 300f);
            bannerRoot.anchoredPosition = new Vector2(0f, 120f);

            bannerStripe = UIBuild.Image("Stripe", bannerRoot, FxSprites.Pixel, new Color(0.02f, 0.05f, 0.18f, 0.7f));
            RectTransform srt = bannerStripe.rectTransform;
            srt.anchorMin = new Vector2(0f, 0.5f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.sizeDelta = new Vector2(0f, 260f);

            bannerTitle = MakeText("Title", bannerRoot, 120f, new Color(1f, 0.9f, 0.3f));
            bannerTitle.rectTransform.anchoredPosition = new Vector2(0f, 35f);
            bannerTitle.outlineWidth = 0.25f;
            bannerTitle.outlineColor = new Color32(120, 40, 0, 255);

            bannerSubtitle = MakeText("Subtitle", bannerRoot, 52f, Color.white);
            bannerSubtitle.rectTransform.anchoredPosition = new Vector2(0f, -70f);

            bannerRoot.gameObject.SetActive(false);
        }

        private void BuildPraise()
        {
            praiseText = MakeText("Praise", transform, 110f, Color.white);
            RectTransform rt = praiseText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.38f);
            rt.sizeDelta = new Vector2(1000f, 200f);
            praiseText.outlineWidth = 0.28f;
            praiseText.outlineColor = new Color32(30, 10, 60, 255);
            praiseText.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI MakeText(string n, Transform parent, float size, Color color)
        {
            RectTransform rt = UIBuild.Rect(n, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000f, size * 1.4f);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        // =====================================================================
        // Top bar
        // =====================================================================

        public void UpdateLevelText(int level)
        {
            if (levelText != null)
            {
                levelText.text = "LEVEL " + level;
                Punch(levelText.rectTransform, 0.18f);
            }
        }

        public void UpdateScoreText(int score)
        {
            if (scoreText == null) return;

            targetScore = score;

            // Reset (naya level) par foran
            if (score <= 0 || !isActiveAndEnabled)
            {
                shownScore = score;
                scoreText.text = "SCORE: " + score;
                return;
            }

            if (scoreRoutine == null)
                scoreRoutine = StartCoroutine(CountScore());

            Punch(scoreText.rectTransform, 0.22f);
        }

        private IEnumerator CountScore()
        {
            while (shownScore != targetScore)
            {
                int diff = targetScore - shownScore;
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(diff) * 0.18f));
                shownScore += diff > 0 ? Mathf.Min(step, diff) : -Mathf.Min(step, -diff);
                scoreText.text = "SCORE: " + shownScore;
                yield return new WaitForSecondsRealtime(0.02f);
            }
            scoreRoutine = null;
        }

        private void Punch(RectTransform rt, float amount)
        {
            if (rt == null || !isActiveAndEnabled) return;
            StartCoroutine(PunchRoutine(rt, amount));
        }

        private static IEnumerator PunchRoutine(RectTransform rt, float amount)
        {
            const float dur = 0.3f;
            float t = 0f;
            while (t < dur && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = 1f + Mathf.Sin(k * Mathf.PI) * amount * (1f - k * 0.5f);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
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

        // =====================================================================
        // Instruction
        // =====================================================================

        public void SetInstructionText(string text, bool visible = true)
        {
            if (instructionText == null) return;

            instructionText.text = text;
            instructionText.gameObject.SetActive(true);
            instructionText.enabled = true;

            if (visible && instructionHidden)
                ShowInstruction();
        }

        /// <summary>Pehle shot ke baad hidayat (instruction) dheere se ghaib.</summary>
        public void HideInstruction()
        {
            if (instructionText == null || instructionHidden) return;
            instructionHidden = true;
            RestartInstructionRoutine(FadeInstruction(0f, 0.35f));
        }

        public void ShowInstruction()
        {
            if (instructionText == null) return;
            instructionHidden = false;
            RestartInstructionRoutine(FadeInstruction(instructionBaseAlpha, 0.35f));
        }

        private void RestartInstructionRoutine(IEnumerator r)
        {
            if (!isActiveAndEnabled) return;
            if (instructionRoutine != null) StopCoroutine(instructionRoutine);
            instructionRoutine = StartCoroutine(r);
        }

        private IEnumerator FadeInstruction(float to, float dur)
        {
            float from = instructionText.alpha;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                instructionText.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            instructionText.alpha = to;

            // Dikhai de raha ho to halka sa saans lena (breathing)
            while (!instructionHidden)
            {
                instructionText.alpha = instructionBaseAlpha * (0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f));
                yield return null;
            }
            instructionRoutine = null;
        }

        // =====================================================================
        // Level banner + praise
        // =====================================================================

        /// <summary>Level shuru: "LEVEL 5" + theme ka naam beech mein se guzarta hai.</summary>
        public void ShowLevelBanner(int level, string subtitle)
        {
            if (bannerRoot == null || !isActiveAndEnabled) return;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine(level, subtitle));
        }

        private IEnumerator BannerRoutine(int level, string subtitle)
        {
            bannerRoot.gameObject.SetActive(true);
            bannerTitle.text = "LEVEL " + level;
            bannerSubtitle.text = subtitle;

            float w = canvasRect != null ? canvasRect.rect.width : 1080f;
            RectTransform tt = bannerTitle.rectTransform;
            RectTransform st = bannerSubtitle.rectTransform;
            RectTransform stripe = bannerStripe.rectTransform;

            // 1) andar aana
            float t = 0f;
            const float inDur = 0.45f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / inDur);
                float e = Ease.OutBack(k, 1.4f);
                tt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-w, 0f, e), 35f);
                st.anchoredPosition = new Vector2(Mathf.LerpUnclamped(w, 0f, Ease.OutBack(Mathf.Clamp01(k * 1.2f - 0.2f), 1.4f)), -70f);
                stripe.localScale = new Vector3(1f, Ease.OutCubic(k), 1f);
                SetBannerAlpha(Mathf.Clamp01(k * 2f));
                yield return null;
            }

            // 2) rukna (halka sa jhoolna)
            t = 0f;
            while (t < 1.0f)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(t * 6f) * 0.02f;
                tt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            tt.localScale = Vector3.one;

            // 3) bahar jaana
            t = 0f;
            const float outDur = 0.35f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Ease.InCubic(Mathf.Clamp01(t / outDur));
                tt.anchoredPosition = new Vector2(Mathf.Lerp(0f, w, k), 35f);
                st.anchoredPosition = new Vector2(Mathf.Lerp(0f, -w, k), -70f);
                stripe.localScale = new Vector3(1f, 1f - k, 1f);
                SetBannerAlpha(1f - k);
                yield return null;
            }

            bannerRoot.gameObject.SetActive(false);
            bannerRoutine = null;
        }

        private void SetBannerAlpha(float a)
        {
            bannerTitle.alpha = a;
            bannerSubtitle.alpha = a;
            Color c = bannerStripe.color;
            c.a = 0.7f * a;
            bannerStripe.color = c;
        }

        /// <summary>Bara combo / bari pop par "AMAZING!" jaisa text.</summary>
        public void ShowPraise(string text, Color color)
        {
            if (praiseText == null || !isActiveAndEnabled) return;
            if (praiseRoutine != null) StopCoroutine(praiseRoutine);
            praiseRoutine = StartCoroutine(PraiseRoutine(text, color));
        }

        private IEnumerator PraiseRoutine(string text, Color color)
        {
            praiseText.gameObject.SetActive(true);
            praiseText.text = text;
            praiseText.color = color;
            RectTransform rt = praiseText.rectTransform;

            const float dur = 1.1f;
            float t = 0f;
            float tilt = UnityEngine.Random.Range(-6f, 6f);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float pop = k < 0.25f ? Ease.OutBack(k / 0.25f, 3f) : 1f;
                rt.localScale = Vector3.one * pop;
                rt.localRotation = Quaternion.Euler(0f, 0f, tilt * (1f - k));
                rt.anchoredPosition = new Vector2(0f, Ease.OutCubic(k) * 90f);
                praiseText.alpha = k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f;
                yield return null;
            }

            praiseText.gameObject.SetActive(false);
            praiseRoutine = null;
        }

        // =====================================================================
        // Panels
        // =====================================================================

        public void HideAllPanels()
        {
            if (panelRoutine != null)
            {
                StopCoroutine(panelRoutine);
                panelRoutine = null;
            }

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
                    winScoreText.text = "FINAL SCORE: \n0";

                PlayPanelIntro(winPanel, false);

                if (isActiveAndEnabled)
                    StartCoroutine(CountFinalScore(winScoreText, finalScore, 0.45f));

                RectTransform root = canvasRect != null ? canvasRect : transform as RectTransform;
                UIConfetti.Burst(root, 110);
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

                PlayPanelIntro(losePanel, true);
            }
        }

        public void ShowPausePanel(bool show)
        {
            if (pausePanel == null) return;

            pausePanel.SetActive(show);
            if (show) PlayPanelIntro(pausePanel, false);
        }

        private IEnumerator CountFinalScore(TextMeshProUGUI text, int target, float delay)
        {
            if (text == null) yield break;

            yield return new WaitForSecondsRealtime(delay);

            const float dur = 0.9f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                int v = Mathf.RoundToInt(target * Ease.OutCubic(t / dur));
                text.text = "FINAL SCORE: \n" + v;
                yield return null;
            }
            text.text = "FINAL SCORE: \n" + target;
            StartCoroutine(PunchRoutine(text.rectTransform, 0.2f));
        }

        private void PlayPanelIntro(GameObject panelRoot, bool shake)
        {
            if (!animatePanels || panelRoot == null || !isActiveAndEnabled) return;
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = StartCoroutine(PanelIntro(panelRoot, shake));
        }

        /// <summary>
        /// Parda fade-in, panel "pop" ke saath bara, phir buttons ek ek kar ke uchhalte hain.
        /// Time.timeScale = 0 (pause) mein bhi chalta hai.
        /// </summary>
        private IEnumerator PanelIntro(GameObject panelRoot, bool shake)
        {
            Image dim = panelRoot.GetComponent<Image>();
            float dimTarget = 0f;
            if (dim != null && !dimAlpha.TryGetValue(dim, out dimTarget))
                dimTarget = dim.color.a;

            // Asal panel = sab se bara bachcha (WinPanel / LosePanel / PausePanel)
            RectTransform panel = FindMainChild(panelRoot.transform);
            Button[] buttons = panel != null
                ? panel.GetComponentsInChildren<Button>(false)
                : new Button[0];

            Vector3[] buttonScales = new Vector3[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                // Pulse animation pop ke dauran scale na chheene
                UIPulse pulse = buttons[i].GetComponent<UIPulse>();
                if (pulse != null) pulse.enabled = false;

                buttonScales[i] = Vector3.one;
                buttons[i].transform.localScale = Vector3.zero;
            }

            Vector2 panelPos = panel != null ? panel.anchoredPosition : Vector2.zero;

            const float dur = 0.42f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);

                if (dim != null)
                {
                    Color c = dim.color;
                    c.a = dimTarget * Mathf.Clamp01(k * 2.5f);
                    dim.color = c;
                }

                if (panel != null)
                {
                    float s = Mathf.LerpUnclamped(0.55f, 1f, Ease.OutBack(k, 2f));
                    panel.localScale = new Vector3(s, s, 1f);
                    panel.anchoredPosition = panelPos + new Vector2(0f, (1f - Ease.OutCubic(k)) * -120f);
                }
                yield return null;
            }

            if (dim != null)
            {
                Color c = dim.color;
                c.a = dimTarget;
                dim.color = c;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
                panel.anchoredPosition = panelPos;
            }

            // Fail par panel "na" mein sar hilata hai
            if (shake && panel != null)
            {
                t = 0f;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = t / 0.4f;
                    panel.anchoredPosition = panelPos + new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * 22f * (1f - k), 0f);
                    yield return null;
                }
                panel.anchoredPosition = panelPos;
            }

            // Buttons ek ek kar ke
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                StartCoroutine(PopButton(buttons[i].transform, buttonScales[i]));
                yield return new WaitForSecondsRealtime(0.08f);
            }

            panelRoutine = null;
        }

        private static IEnumerator PopButton(Transform tr, Vector3 target)
        {
            const float dur = 0.35f;
            float t = 0f;
            while (t < dur && tr != null)
            {
                t += Time.unscaledDeltaTime;
                tr.localScale = target * Ease.OutBack(Mathf.Clamp01(t / dur), 2.6f);
                yield return null;
            }
            if (tr != null)
            {
                tr.localScale = target;
                UIPulse pulse = tr.GetComponent<UIPulse>();
                if (pulse != null) pulse.enabled = true;
            }
        }

        private static RectTransform FindMainChild(Transform root)
        {
            RectTransform best = null;
            float bestArea = -1f;
            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform rt = root.GetChild(i) as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf) continue;
                if (rt.name == "Sunburst" || rt.name == "Confetti") continue;
                float area = Mathf.Abs(rt.rect.width * rt.rect.height);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = rt;
                }
            }
            return best;
        }
    }
}
