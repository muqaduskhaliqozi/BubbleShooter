
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void PlayButton()
    {
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

        SceneManager.LoadScene(gameplaySceneName);
    }
}
