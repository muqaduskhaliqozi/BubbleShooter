using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BubbleShooter.FX;

/// <summary>
/// Loading screen (mockup ke mutabiq): beech mein logo, upar daayein se roshni ki shuaaein,
/// tairti / dhundli bubbles, peela loading bar jismein %, aur neeche bulb ke saath "Tip".
/// Scene ke bg aur logo dobara istemal hote hain; purana loading bar chhupa kar naya banta hai.
/// </summary>
public class SplashLoading : MonoBehaviour
{
    [Header("Loading Bar")]
    [SerializeField] private Slider loadingSlider;

    [Tooltip("Optional: Image must have Image Type = Filled")]
    [SerializeField] private Image fillImage;

    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Loading Settings")]
    [SerializeField] private float loadDuration = 5f;

    [Tooltip("Name of the scene that should open after Splash")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Mockup Layout (runtime)")]
    [Tooltip("ON: loading screen ko naye mockup design mein code se sajaya jata hai.")]
    [SerializeField] private bool decorate = true;

    [SerializeField]
    private string[] tips =
    {
        "Tip: Match 3 or more bubbles!",
        "Tip: Bounce shots off the walls!",
        "Tip: Tap the next bubble to swap!",
        "Tip: Drop big clusters for bonus points!",
        "Tip: Keep bubbles above the danger line!"
    };

    private static readonly Color32 NavyOutline = new Color32(8, 30, 95, 255);

    private AsyncOperation loadingOperation;

    // runtime pieces
    private RectTransform canvasRect;
    private RectTransform barFill;
    private float barInnerWidth;
    private Image rays;
    private RectTransform tipGroup;
    private TextMeshProUGUI tipText;
    private RectTransform bulb;
    private Image fadeOverlay;
    private int tipIndex;
    private float tipTimer;

    private IEnumerator Start()
    {
        if (decorate)
            BuildDecorations();

        // Start from 0%
        SetProgress(0f);

        // Start loading the next scene in the background
        loadingOperation = SceneManager.LoadSceneAsync(nextSceneName);

        if (loadingOperation == null)
        {
            Debug.LogError(
                "[SplashLoading] Could not load scene: " + nextSceneName +
                ". Make sure the scene name is correct and added to Build Settings."
            );

            yield break;
        }

        // Don't activate the next scene automatically
        loadingOperation.allowSceneActivation = false;

        float timer = 0f;

        // Smooth loading bar
        while (timer < loadDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / loadDuration);

            // Smooth progress
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Asli loading 90% par rukti hai; bar usse aage bhaage nahi jab tak load na ho
            float real = Mathf.Clamp01(loadingOperation.progress / 0.9f);
            smoothProgress = Mathf.Min(smoothProgress, Mathf.Max(real, 0.95f));

            SetProgress(smoothProgress);

            AnimateStatus();

            yield return null;
        }

        // Unity normally stops AsyncOperation at 0.9 until activation
        while (loadingOperation.progress < 0.9f)
        {
            AnimateStatus();

            yield return null;
        }

        // Make sure bar reaches 100%
        SetProgress(1f);

        // Small delay so 100% is visible
        yield return new WaitForSeconds(0.25f);

        // Dheere se andhera, phir agla scene (MainMenu wahan se roshan karta hai)
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            float t = 0f;
            const float dur = 0.35f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                Color c = fadeOverlay.color;
                c.a = Mathf.Clamp01(t / dur);
                fadeOverlay.color = c;
                yield return null;
            }
        }

        // Activate next scene
        loadingOperation.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);

        // Slider
        if (loadingSlider != null)
        {
            loadingSlider.value = value;
        }

        // Filled Image
        if (fillImage != null)
        {
            fillImage.fillAmount = value;
        }

        // Naya (mockup wala) bar: fill ki chaurai barhti hai.
        // Kam se kam gol sirey jitna (height) taake shape na bigde.
        if (barFill != null)
        {
            float h = barFill.sizeDelta.y;
            float w = Mathf.Lerp(h, barInnerWidth, value);
            barFill.sizeDelta = new Vector2(w, h);
            barFill.gameObject.SetActive(value > 0.005f);
        }

        // Percentage text
        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void AnimateStatus()
    {
        if (statusText == null)
            return;

        int dots = Mathf.FloorToInt(Time.time * 3f) % 4;

        statusText.text = "LOADING" + new string('.', dots);
    }

    // =====================================================================
    // Mockup layout (sab code se, scene mein kuch badalne ki zaroorat nahi)
    // =====================================================================

    private void BuildDecorations()
    {
        Canvas canvas = FindSplashCanvas();
        if (canvas == null) return;

        UIArt.ConfigureScaler(canvas);
        canvasRect = canvas.transform as RectTransform;

        Image bg = FindImage("bg");
        Image logo = FindImage("logo");

        // 1) Background poori screen par (aspect bachaa kar)
        if (bg != null)
        {
            UIArt.CoverScreen(bg);
            bg.transform.SetAsFirstSibling();
        }

        // 2) Upar daayein kone se roshni ki shuaaein
        rays = UIArt.Image("LightRays", canvasRect, "loading_light_rays", new Vector2(1250f, 1250f));
        RectTransform rrt = rays.rectTransform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(1f, 1f);
        rrt.pivot = new Vector2(1f, 1f);
        rrt.anchoredPosition = new Vector2(40f, 40f);
        rays.color = new Color(1f, 1f, 1f, 0.75f);
        rrt.SetSiblingIndex(1);

        // 3) Peeche tairti chamakdar bubbles
        RectTransform floatLayer = UIBuild.Stretch("FloatingBubbles", canvasRect);
        floatLayer.SetSiblingIndex(2);
        UIFloatingBubbles fb = floatLayer.gameObject.AddComponent<UIFloatingBubbles>();
        fb.count = 12;
        fb.sizeRange = new Vector2(0.045f, 0.12f);
        fb.speedRange = new Vector2(0.03f, 0.08f);
        fb.alpha = 0.95f;
        fb.sizeFade = 0.15f;
        fb.popOnTap = true;

        // 4) Logo beech mein
        if (logo != null)
        {
            logo.preserveAspect = true;
            UIArt.Place(logo.rectTransform, new Vector2(0.5f, 0.645f), new Vector2(780f, 470f), Vector2.zero);
            logo.transform.SetAsLastSibling();
            StartCoroutine(LogoIntro(logo.rectTransform));
        }

        // 5) Purana loading bar chhupao, naya banao
        Transform oldBar = canvasRect.Find("LoadingBar");
        if (oldBar != null) oldBar.gameObject.SetActive(false);
        fillImage = null;
        loadingSlider = null;

        BuildLoadingBar();
        BuildTip();

        // 6) Aage (foreground) bari dhundli bubbles - kinaron par, gehrai ka ehsaas
        RectTransform front = UIBuild.Stretch("ForegroundBokeh", canvasRect);
        UIFloatingBubbles fg = front.gameObject.AddComponent<UIFloatingBubbles>();
        fg.bodySprite = UIArt.Get("loading_bokeh_bubble");
        fg.count = 5;
        fg.sizeRange = new Vector2(0.2f, 0.34f);
        fg.speedRange = new Vector2(0.015f, 0.035f);
        fg.alpha = 0.8f;
        fg.sizeFade = 0.25f;
        fg.edgeBias = 1f;
        fg.popOnTap = false;

        // 7) Fade overlay (sab se upar)
        fadeOverlay = UIBuild.Image("Fade", canvasRect, FxSprites.Pixel, new Color(0.02f, 0.05f, 0.16f, 0f));
        RectTransform frt = fadeOverlay.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        frt.SetAsLastSibling();
    }

    private static Canvas FindSplashCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            Canvas r = c.rootCanvas;
            if (r.GetComponent<SceneFader>() != null) continue;
            if (r.transform.Find("logo") != null) return r;
            if (fallback == null) fallback = r;
        }
        return fallback;
    }

    private Image FindImage(string name)
    {
        Transform t = canvasRect.Find(name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private void BuildLoadingBar()
    {
        const float barW = 640f;
        const float barH = 92f;
        const float inset = 11f;

        Image frame = UIArt.Image("LoadingBarFrame", canvasRect, "loading_bar_frame", new Vector2(barW, barH));
        UIArt.Place(frame.rectTransform, new Vector2(0.5f, 0.378f), new Vector2(barW, barH), Vector2.zero);

        float innerH = barH - inset * 2f;
        barInnerWidth = barW - inset * 2f;

        Image fill = UIArt.Image("Fill", frame.transform, "loading_bar_fill", new Vector2(innerH, innerH));
        barFill = fill.rectTransform;
        barFill.anchorMin = barFill.anchorMax = new Vector2(0f, 0.5f);
        barFill.pivot = new Vector2(0f, 0.5f);
        barFill.anchoredPosition = new Vector2(inset, 0f);
        barFill.sizeDelta = new Vector2(innerH, innerH);

        percentText = UIArt.Text("Percent", frame.transform, 50f, Color.white, NavyOutline, 0.26f);
        RectTransform prt = percentText.rectTransform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(0f, 4f);
        prt.offsetMax = Vector2.zero;

        UIPopIn pop = frame.gameObject.AddComponent<UIPopIn>();
        pop.delay = 0.35f;
    }

    private void BuildTip()
    {
        tipGroup = UIBuild.Rect("Tip", canvasRect);
        UIArt.Place(tipGroup, new Vector2(0.5f, 0.322f), new Vector2(900f, 80f), Vector2.zero);

        Image b = UIArt.Image("Bulb", tipGroup, "loading_bulb", new Vector2(66f, 66f));
        bulb = b.rectTransform;

        tipText = UIArt.Text("Text", tipGroup, 40f, Color.white, NavyOutline, 0.18f);
        tipText.rectTransform.sizeDelta = new Vector2(820f, 70f);

        tipIndex = 0;
        ShowTip();
    }

    private void ShowTip()
    {
        if (tipText == null || tips == null || tips.Length == 0) return;

        tipText.text = tips[tipIndex % tips.Length];

        // Bulb + text ko ek saath beech mein rakho
        float textW = Mathf.Min(820f, tipText.GetPreferredValues(tipText.text).x);
        const float bulbW = 66f;
        const float gap = 14f;
        float total = bulbW + gap + textW;
        bulb.anchoredPosition = new Vector2(-total * 0.5f + bulbW * 0.5f, 4f);
        tipText.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + bulbW + gap + textW * 0.5f, 0f);
    }

    private IEnumerator LogoIntro(RectTransform logoRect)
    {
        Vector2 basePos = logoRect.anchoredPosition;
        Vector2 from = basePos + new Vector2(0f, 1100f);

        float t = 0f;
        const float dur = 0.9f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            logoRect.anchoredPosition = Vector2.LerpUnclamped(from, basePos, Ease.OutBack(k, 1.6f));

            // utarne par squash
            float sq = k > 0.55f ? Mathf.Sin((k - 0.55f) / 0.45f * Mathf.PI) * 0.12f : 0f;
            logoRect.localScale = new Vector3(1f + sq, 1f - sq, 1f);
            yield return null;
        }

        logoRect.anchoredPosition = basePos;
        logoRect.localScale = Vector3.one;

        UIBob bob = logoRect.gameObject.AddComponent<UIBob>();
        bob.amplitude = 10f;
        bob.speed = 1.8f;
        bob.rotationAmplitude = 1.2f;
    }

    private void Update()
    {
        if (!decorate) return;

        // Roshni halke se dhadakti hai
        if (rays != null)
        {
            float a = 0.62f + 0.18f * Mathf.Sin(Time.unscaledTime * 1.1f);
            rays.color = new Color(1f, 1f, 1f, a);
            rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.4f) * 2.5f);
        }

        // Bulb timtimata hai
        if (bulb != null)
        {
            float s = 1f + Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 4f)) * 0.08f;
            bulb.localScale = new Vector3(s, s, 1f);
        }

        // Tips badalte rehte hain (fade ke saath)
        if (tipText != null && tips != null && tips.Length > 1)
        {
            tipTimer += Time.unscaledDeltaTime;
            const float cycle = 2.6f;
            float fade = Mathf.Clamp01(Mathf.Min(tipTimer, cycle - tipTimer) / 0.3f);
            tipText.alpha = fade;
            if (tipTimer >= cycle)
            {
                tipTimer = 0f;
                tipIndex = (tipIndex + 1) % tips.Length;
                ShowTip();
            }
        }
    }
}
