using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

[Serializable]
public class GameVideoAdData
{
    public string targetGameAsin;
    public VideoClip videoClip;
}

public class VideoPlaybackAd : MonoBehaviour
{
    [SerializeField] private GameVideoAdData[] gamesVideoAdData;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject closeButton;
    private bool _closeButtonTriggered = false;

    [SerializeField] private float closeBtnDelay = 5f; // Show after 3 seconds
    [SerializeField] private float AdKitDelay = 10f; // Show after 3 seconds
    private string _currentGameAsin;
    private Coroutine _closeButtonCoroutine;
    static bool FirstTime = true;
    private void OnEnable()
    {
        var index = 0;

        Time.timeScale = 0f;


        if (FirstTime)
        {
            index = 2;
        }
        else
        {
            index = Random.Range(0, gamesVideoAdData.Length);
        }
        FirstTime = false;

        _currentGameAsin = gamesVideoAdData[index].targetGameAsin;

        closeButton.SetActive(false); // Hide immediately
        videoPlayer.clip = gamesVideoAdData[index].videoClip;

        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepared;

        // Start coroutine to show close button after delay
        _closeButtonCoroutine = StartCoroutine(ShowCloseButtonAfterDelay());
    }

    private void OnPrepared(VideoPlayer vp)
    {
        rawImage.texture = vp.texture;
        vp.Play();
    }

    private System.Collections.IEnumerator ShowCloseButtonAfterDelay()
    {

        yield return new WaitForSecondsRealtime(AdKitDelay);
        LaunchAmazonPurchase();
        yield return new WaitForSecondsRealtime(closeBtnDelay);
        closeButton.SetActive(true);
        _closeButtonTriggered = true;
    }

    public void Close()
    {
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void LaunchAmazonPurchase()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsAmazonAppstoreInstalled())
        {
            string deepLink = $"amzn://apps/android?initiatePurchaseFlow=true&asin={_currentGameAsin}";
            Application.OpenURL(deepLink);
        }
        else
        {
            string regionalDomain = GetRegionalAmazonDomain();
            string fallbackUrl = $"https://{regionalDomain}/gp/mas/dl/android?asin={_currentGameAsin}";
            Application.OpenURL(fallbackUrl);
        }
#else
        Debug.Log("Amazon deep link can only be tested on an Android device.");
#endif
    }

    private bool IsAmazonAppstoreInstalled()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
            {
                AndroidJavaObject intent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", "com.amazon.venezia");
                return intent != null;
            }
        }
        catch
        {
            return false;
        }
    }

    private string GetRegionalAmazonDomain()
    {
        var systemLang = Application.systemLanguage;

        return systemLang switch
        {
            SystemLanguage.German => "amazon.de",
            SystemLanguage.French => "amazon.fr",
            SystemLanguage.Japanese => "amazon.co.jp",
            SystemLanguage.Italian => "amazon.it",
            SystemLanguage.Spanish => "amazon.es",
            SystemLanguage.Portuguese => "amazon.com.br",
            SystemLanguage.English => "amazon.com",
            _ => "amazon.com"
        };
    }

    private void OnDisable()
    {

        Time.timeScale = 1f;
        _closeButtonTriggered = false;
        if (_closeButtonCoroutine != null)
        {
            StopCoroutine(_closeButtonCoroutine);
        }
    }
}