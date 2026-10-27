using UnityEngine;

public class CheckCpAd : MonoBehaviour
{
    public static CheckCpAd instance;

    [SerializeField] private float adReadyTime = 3f;
    [SerializeField] private GameObject cpVideoAd;

    private float timer = 0f;
    private bool isAdReady = false;

    public bool IsAdReady => isAdReady;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        timer = 0f;

        if (cpVideoAd != null)
            cpVideoAd.SetActive(false);

        Debug.Log("Ad timer started: " + adReadyTime);
    }


    private void Update()
    {
        if (!isAdReady)
        {
            timer += Time.deltaTime;

            if (timer >= adReadyTime)
            {
                isAdReady = true;
                Debug.Log("Ad is ready");
            }
        }
    }


    public void CallAd()
    {
        if (!isAdReady)
        {
            Debug.Log("Ad not ready yet.");
            return;
        }


        if (cpVideoAd != null)
        {
            Debug.Log("Ad called");

            cpVideoAd.SetActive(true);

            // reset timer
            isAdReady = false;
            timer = 0f;
        }
    }


    public void CloseAd()
    {
        if (cpVideoAd != null)
        {
            cpVideoAd.SetActive(false);
        }

        Debug.Log("Ad Closed");
    }
}