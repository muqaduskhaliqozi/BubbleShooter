using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

    private AsyncOperation loadingOperation;

    private IEnumerator Start()
    {
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

            SetProgress(smoothProgress);

            AnimateStatus();

            yield return null;
        }

        // Make sure bar reaches 100%
        SetProgress(1f);

        // Unity normally stops AsyncOperation at 0.9 until activation
        while (loadingOperation.progress < 0.9f)
        {
            AnimateStatus();

            yield return null;
        }

        // Small delay so 100% is visible
        yield return new WaitForSeconds(0.2f);

        // Activate Gameplay scene
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
}