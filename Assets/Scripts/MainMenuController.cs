
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BubbleShooter.Core;
using BubbleShooter.FX;

/// <summary>
/// Main Menu (mockup ke mutabiq): upar baayein Settings, daayein Coins, beech mein logo,
/// neeche lakdi ke frame wala peela PLAY button jiske upar bubble mascot jhaank raha hai.
/// Scene ke purane objects (bg, logo, PlayButton) dobara istemal hote hain; baaqi sab code se banta hai.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Mockup Layout (runtime)")]
    [Tooltip("ON: menu ko naye mockup design mein code se sajaya jata hai.")]
    [SerializeField] private bool decorate = true;

    private static readonly Color32 NavyOutline = new Color32(8, 30, 95, 255);

    private bool starting;

    private RectTransform root;
    private TextMeshProUGUI coinText;
    private RectTransform settingsPanel;
    private CanvasGroup settingsGroup;
    private TextMeshProUGUI toastText;
    private Coroutine toastRoutine;
    private Coroutine settingsRoutine;

    private void Start()
    {
        GameSettings.Apply();

        // Pehle menu banao (SceneFader ka apna Canvas baad mein bane, taake galat Canvas na mile)
        if (decorate)
            BuildMenu();

        // Splash / gameplay se andhere ke baad roshni wapas
        SceneFader.FadeInFromBlack();
    }

    private void OnDestroy()
    {
        CoinWallet.OnChanged -= OnCoinsChanged;
    }

    public void PlayButton()
    {
        if (starting)
            return;

        Debug.Log("[MainMenuController] Play button clicked.");

        // CP Ad on Play button
        if (CheckCpAd.instance != null)
        {
            CheckCpAd.instance.CallAd();
        }
        else
        {
            Debug.LogWarning("[MainMenuController] CheckCpAd instance is NULL.");
        }

        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[MainMenuController] Gameplay scene name is empty.");
            return;
        }

        starting = true;
        SceneFader.LoadScene(gameplaySceneName);
    }

    // =====================================================================
    // Layout
    // =====================================================================

    private void BuildMenu()
    {
        Canvas canvas = FindMenuCanvas();
        if (canvas == null) return;
        UIArt.ConfigureScaler(canvas);

        root = canvas.transform as RectTransform;

        Image bg = FindImage("bg");
        Image logo = FindImage("logo");
        Button play = FindButton("PlayButton");

        // 1) Background: poori screen, aspect bachaa kar
        if (bg != null)
        {
            UIArt.CoverScreen(bg);
            bg.transform.SetAsFirstSibling();
        }

        // 2) Tairti bubbles (chhoo kar phodi ja sakti hain)
        RectTransform floatLayer = UIBuild.Stretch("FloatingBubbles", root);
        floatLayer.SetSiblingIndex(1);
        UIFloatingBubbles fb = floatLayer.gameObject.AddComponent<UIFloatingBubbles>();
        fb.count = 10;
        fb.sizeRange = new Vector2(0.06f, 0.11f);
        fb.speedRange = new Vector2(0.03f, 0.07f);
        fb.alpha = 0.95f;
        fb.sizeFade = 0.15f;
        fb.popOnTap = true;

        // 3) Logo: upar ke teesre hisse mein
        if (logo != null)
        {
            logo.preserveAspect = true;
            UIArt.Place(logo.rectTransform, new Vector2(0.5f, 0.69f), new Vector2(900f, 540f), Vector2.zero);
            logo.transform.SetAsLastSibling();
            StartCoroutine(DropIn(logo.rectTransform, 900f, 0.1f));
        }

        // 4) Mascot (button ke peeche), PLAY button, haath (button ke aage)
        Image mascot = UIArt.Image("Mascot", root, "menu_mascot", new Vector2(420f, 330f));
        UIArt.Place(mascot.rectTransform, new Vector2(0.5f, 0.473f), new Vector2(420f, 330f), new Vector2(0f, 35f));
        mascot.gameObject.AddComponent<UIMascotIdle>();

        if (play != null)
        {
            StyleTheButton(play);
        }

        Image hands = UIArt.Image("MascotHands", root, "menu_mascot_hands", new Vector2(420f, 90f));
        UIArt.Place(hands.rectTransform, new Vector2(0.5f, 0.348f), new Vector2(420f, 90f), new Vector2(0f, 138f));

        StartCoroutine(MascotPeek(mascot.rectTransform, hands.rectTransform));

        // 5) Upar wali patti (safe area ke andar): Settings + Coins
        RectTransform safe = UIArt.SafeArea(root);
        BuildSettingsButton(safe);
        BuildCoinCounter(safe);

        // 6) Settings popup + chhota sa toast
        BuildSettingsPanel();
        BuildToast();
    }

    private static Canvas FindMenuCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas c in FindObjectsOfType<Canvas>())
        {
            Canvas r = c.rootCanvas;
            if (r.GetComponent<SceneFader>() != null) continue;
            if (r.transform.Find("PlayButton") != null) return r;
            if (fallback == null) fallback = r;
        }
        return fallback;
    }

    private Image FindImage(string name)
    {
        Transform t = root.Find(name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private Button FindButton(string name)
    {
        Transform t = root.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    /// <summary>Scene wale PlayButton ko naya roop do (onClick waisa hi rehta hai).</summary>
    private void StyleTheButton(Button play)
    {
        Image img = play.GetComponent<Image>();
        if (img != null)
        {
            Sprite s = UIArt.Get("menu_play_button");
            if (s != null) img.sprite = s;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }

        // Rang badalne wala transition band (naya sprite khud chamakdar hai)
        play.transition = Selectable.Transition.None;

        UIArt.Place(play.transform as RectTransform, new Vector2(0.5f, 0.348f), new Vector2(870f, 348f), Vector2.zero);
        play.transform.SetAsLastSibling();

        TextMeshProUGUI label = play.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
            label = UIArt.Text("Label", play.transform, 120f, Color.white, NavyOutline);

        UIArt.ApplyFont(label);
        label.text = "PLAY";
        label.fontSize = 118f;
        label.color = new Color32(92, 44, 8, 255);
        label.outlineWidth = 0f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.raycastTarget = false;
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(0f, 10f);
        lrt.offsetMax = new Vector2(0f, 0f);

        if (play.GetComponent<UIButtonJuice>() == null)
            play.gameObject.AddComponent<UIButtonJuice>();

        StartCoroutine(PopThenPulse(play.transform as RectTransform, 0.45f));
    }

    private void BuildSettingsButton(RectTransform safe)
    {
        Image img = UIArt.Image("SettingsButton", safe, "menu_settings_button", new Vector2(150f, 150f));
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(105f, -95f);
        img.raycastTarget = true;

        Button b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(ToggleSettings);
        img.gameObject.AddComponent<UIButtonJuice>();

        UIPopIn pop = img.gameObject.AddComponent<UIPopIn>();
        pop.delay = 0.6f;
    }

    private void BuildCoinCounter(RectTransform safe)
    {
        RectTransform group = UIBuild.Rect("CoinCounter", safe);
        group.anchorMin = group.anchorMax = new Vector2(1f, 1f);
        group.sizeDelta = new Vector2(330f, 120f);
        group.anchoredPosition = new Vector2(-205f, -95f);

        Image pill = UIArt.Image("Pill", group, "menu_coin_pill", new Vector2(280f, 92f));
        pill.rectTransform.anchoredPosition = new Vector2(10f, 0f);

        Image coin = UIArt.Image("Coin", group, "menu_coin", new Vector2(120f, 120f));
        coin.rectTransform.anchoredPosition = new Vector2(-125f, 0f);
        UIBob coinBob = coin.gameObject.AddComponent<UIBob>();
        coinBob.amplitude = 4f;
        coinBob.speed = 2.4f;
        coinBob.rotationAmplitude = 6f;

        coinText = UIArt.Text("Amount", group, 56f, Color.white, NavyOutline, 0.2f);
        coinText.rectTransform.sizeDelta = new Vector2(170f, 80f);
        coinText.rectTransform.anchoredPosition = new Vector2(10f, 2f);
        coinText.text = CoinWallet.Coins.ToString();

        Image plus = UIArt.Image("Plus", group, "menu_plus_button", new Vector2(82f, 82f));
        plus.rectTransform.anchoredPosition = new Vector2(140f, 0f);
        plus.raycastTarget = true;
        Button pb = plus.gameObject.AddComponent<Button>();
        pb.transition = Selectable.Transition.None;
        pb.onClick.AddListener(() => ShowToast("Clear levels to earn coins!"));
        plus.gameObject.AddComponent<UIButtonJuice>();

        UIPopIn pop = group.gameObject.AddComponent<UIPopIn>();
        pop.delay = 0.7f;

        CoinWallet.OnChanged += OnCoinsChanged;
    }

    private void OnCoinsChanged(int value)
    {
        if (coinText != null)
            coinText.text = value.ToString();
    }

    // =====================================================================
    // Settings popup
    // =====================================================================

    private void BuildSettingsPanel()
    {
        settingsPanel = UIBuild.Stretch("SettingsPopup", root);
        settingsGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();

        Image dim = UIBuild.Image("Dim", settingsPanel, FxSprites.Pixel, new Color(0.02f, 0.04f, 0.14f, 0.75f));
        RectTransform drt = dim.rectTransform;
        drt.anchorMin = Vector2.zero;
        drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dim.raycastTarget = true;
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(ToggleSettings);

        Image panel = UIArt.Image("Panel", settingsPanel, "ui_panel", new Vector2(820f, 700f));
        panel.raycastTarget = true;   // panel par tap se band na ho
        RectTransform prt = panel.rectTransform;

        TextMeshProUGUI title = UIArt.Text("Title", prt, 96f, new Color32(255, 214, 64, 255), new Color32(110, 50, 0, 255), 0.25f);
        title.rectTransform.sizeDelta = new Vector2(700f, 130f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 245f);
        title.text = "SETTINGS";

        BuildToggleRow(prt, "SOUND", 70f, () => GameSettings.SoundOn, v => GameSettings.SoundOn = v);
        BuildToggleRow(prt, "MUSIC", -90f, () => GameSettings.MusicOn, v => GameSettings.MusicOn = v);

        TextMeshProUGUI ver = UIArt.Text("Version", prt, 34f, new Color(1f, 1f, 1f, 0.7f), NavyOutline, 0f);
        ver.rectTransform.sizeDelta = new Vector2(600f, 60f);
        ver.rectTransform.anchoredPosition = new Vector2(0f, -270f);
        ver.text = "v" + Application.version;

        Image close = UIArt.Image("Close", prt, "ui_close_button", new Vector2(130f, 130f));
        close.rectTransform.anchoredPosition = new Vector2(385f, 325f);
        close.raycastTarget = true;
        Button cb = close.gameObject.AddComponent<Button>();
        cb.transition = Selectable.Transition.None;
        cb.onClick.AddListener(ToggleSettings);
        close.gameObject.AddComponent<UIButtonJuice>();

        settingsPanel.gameObject.SetActive(false);
    }

    private void BuildToggleRow(RectTransform panel, string label, float y,
        System.Func<bool> getter, System.Action<bool> setter)
    {
        TextMeshProUGUI t = UIArt.Text(label + "Label", panel, 72f, Color.white, NavyOutline, 0.22f);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.rectTransform.sizeDelta = new Vector2(380f, 110f);
        t.rectTransform.anchoredPosition = new Vector2(-160f, y);
        t.text = label;

        Image toggle = UIArt.Image(label + "Toggle", panel, getter() ? "ui_toggle_on" : "ui_toggle_off", new Vector2(220f, 110f));
        toggle.rectTransform.anchoredPosition = new Vector2(220f, y);
        toggle.raycastTarget = true;

        Image knob = UIBuild.Image("Knob", toggle.transform, BubbleShooter.Bubble.BubbleSpriteFactory.GetBaseSprite(), Color.white);
        knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        knob.rectTransform.sizeDelta = new Vector2(84f, 84f);

        TextMeshProUGUI state = UIArt.Text("State", toggle.transform, 40f, Color.white, NavyOutline, 0.2f);
        state.rectTransform.sizeDelta = new Vector2(110f, 80f);

        System.Action refresh = () =>
        {
            bool on = getter();
            toggle.sprite = UIArt.Get(on ? "ui_toggle_on" : "ui_toggle_off");
            knob.rectTransform.anchoredPosition = new Vector2(on ? 52f : -52f, 2f);
            state.text = on ? "ON" : "OFF";
            state.rectTransform.anchoredPosition = new Vector2(on ? -40f : 40f, 2f);
        };
        refresh();

        Button b = toggle.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() =>
        {
            setter(!getter());
            refresh();
        });
        toggle.gameObject.AddComponent<UIButtonJuice>();
    }

    private void ToggleSettings()
    {
        if (settingsPanel == null) return;
        bool show = !settingsPanel.gameObject.activeSelf;
        if (settingsRoutine != null) StopCoroutine(settingsRoutine);
        settingsRoutine = StartCoroutine(AnimateSettings(show));
    }

    private IEnumerator AnimateSettings(bool show)
    {
        RectTransform panel = settingsPanel.Find("Panel") as RectTransform;
        settingsPanel.gameObject.SetActive(true);
        settingsPanel.SetAsLastSibling();

        const float dur = 0.28f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = show ? Ease.OutBack(k, 2f) : 1f - Ease.InCubic(k);
            settingsGroup.alpha = show ? Mathf.Clamp01(k * 2f) : 1f - k;
            if (panel != null)
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, e);
            yield return null;
        }

        settingsGroup.alpha = show ? 1f : 0f;
        if (panel != null) panel.localScale = Vector3.one;
        if (!show) settingsPanel.gameObject.SetActive(false);
        settingsRoutine = null;
    }

    // =====================================================================
    // Toast
    // =====================================================================

    private void BuildToast()
    {
        toastText = UIArt.Text("Toast", root, 50f, Color.white, NavyOutline, 0.25f);
        RectTransform rt = toastText.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(1000f, 100f);
        toastText.gameObject.SetActive(false);
    }

    private void ShowToast(string message)
    {
        if (toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.gameObject.SetActive(true);
        toastText.transform.SetAsLastSibling();
        toastText.text = message;

        float t = 0f;
        const float dur = 2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            toastText.alpha = k < 0.15f ? k / 0.15f : (k > 0.8f ? (1f - k) / 0.2f : 1f);
            toastText.rectTransform.anchoredPosition = new Vector2(0f, Ease.OutCubic(Mathf.Clamp01(k * 4f)) * 30f);
            yield return null;
        }
        toastText.gameObject.SetActive(false);
        toastRoutine = null;
    }

    // =====================================================================
    // Intro animations
    // =====================================================================

    private IEnumerator DropIn(RectTransform rt, float distance, float delay)
    {
        Vector2 basePos = rt.anchoredPosition;
        Vector2 from = basePos + new Vector2(0f, distance);
        rt.anchoredPosition = from;

        yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        const float dur = 0.85f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, basePos, Ease.OutBack(k, 1.5f));
            float sq = k > 0.5f ? Mathf.Sin((k - 0.5f) / 0.5f * Mathf.PI) * 0.1f : 0f;
            rt.localScale = new Vector3(1f + sq, 1f - sq, 1f);
            yield return null;
        }

        rt.anchoredPosition = basePos;
        rt.localScale = Vector3.one;

        UIBob bob = rt.gameObject.AddComponent<UIBob>();
        bob.amplitude = 10f;
        bob.speed = 1.6f;
        bob.rotationAmplitude = 1.2f;
    }

    /// <summary>Mascot button ke peeche se upar jhaankta hai.</summary>
    private IEnumerator MascotPeek(RectTransform mascot, RectTransform hands)
    {
        // Idle pehle band (OnDisable position reset karta hai), phir neeche chhupao
        UIMascotIdle idle = mascot.GetComponent<UIMascotIdle>();
        if (idle != null) idle.enabled = false;

        Vector2 mPos = mascot.anchoredPosition;
        Vector2 hPos = hands.anchoredPosition;
        mascot.anchoredPosition = mPos - new Vector2(0f, 230f);
        hands.localScale = Vector3.zero;

        yield return new WaitForSecondsRealtime(0.9f);

        float t = 0f;
        const float dur = 0.55f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            mascot.anchoredPosition = Vector2.LerpUnclamped(mPos - new Vector2(0f, 230f), mPos, Ease.OutBack(k, 2f));
            hands.localScale = Vector3.one * Ease.OutBack(Mathf.Clamp01(k * 1.5f - 0.4f), 2.5f);
            yield return null;
        }

        mascot.anchoredPosition = mPos;
        hands.localScale = Vector3.one;
        hands.anchoredPosition = hPos;

        if (idle != null) idle.enabled = true;
    }

    private IEnumerator PopThenPulse(RectTransform rt, float delay)
    {
        Vector3 baseScale = Vector3.one;
        rt.localScale = Vector3.zero;

        yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        const float dur = 0.45f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = baseScale * Ease.OutBack(Mathf.Clamp01(t / dur), 2.4f);
            yield return null;
        }
        rt.localScale = baseScale;

        UIPulse pulse = rt.gameObject.AddComponent<UIPulse>();
        pulse.amount = 0.04f;
        pulse.speed = 2.4f;
    }
}
