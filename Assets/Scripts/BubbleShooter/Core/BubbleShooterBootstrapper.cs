using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.Shooter;
using BubbleShooter.Matching;
using BubbleShooter.Level;
using BubbleShooter.Score;
using BubbleShooter.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BubbleShooter.Core
{
    [ExecuteAlways]
    public class BubbleShooterBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            EnsureHierarchyBuilt();
        }

        private void OnEnable()
        {
            EnsureHierarchyBuilt();
        }

        public void EnsureHierarchyBuilt()
        {
            // Only execute if GameManager doesn't physically exist in scene hierarchy yet
            if (GameObject.Find("GameManager") != null && GameObject.Find("GameplayCanvas") != null)
            {
                return;
            }

            BuildCompleteHierarchy();
        }

        [ContextMenu("Rebuild Scene Hierarchy Physically")]
        public void BuildCompleteHierarchy()
        {
            Debug.Log("[BubbleShooter] Building physical scene hierarchy in GamePlay scene...");

            // 1. Camera setup
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.transform.position = new Vector3(0, 0, -10f);

            // 2. GameplayBackground
            GameObject bgObj = GameObject.Find("GameplayBackground");
            if (bgObj == null)
            {
                bgObj = new GameObject("GameplayBackground");
                SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = CreateSquareSprite();
                bgSr.color = new Color(0.1f, 0.13f, 0.2f, 1f);
                bgSr.sortingOrder = -10;
                bgObj.transform.position = new Vector3(0, 0, 5f);
                bgObj.transform.localScale = new Vector3(15f, 20f, 1f);
            }

            // 3. Create Sprites & Prefabs
            Sprite circleSprite = CreateCircleSprite();
            Sprite squareSprite = CreateSquareSprite();
            GameObject bubblePrefab = CreateBubblePrefabObject(circleSprite);

            // 4. Managers
            GameObject managerObj = GameObject.Find("GameManager");
            if (managerObj == null) managerObj = new GameObject("GameManager");
            
            BubbleShooterGameManager gameManager = GetOrAddComponent<BubbleShooterGameManager>(managerObj);
            BubbleColorGenerator colorGenerator = GetOrAddComponent<BubbleColorGenerator>(managerObj);
            BubbleMatchFinder matchFinder = GetOrAddComponent<BubbleMatchFinder>(managerObj);
            FloatingBubbleDetector floatingDetector = GetOrAddComponent<FloatingBubbleDetector>(managerObj);

            GameObject levelManagerObj = GameObject.Find("LevelManager");
            if (levelManagerObj == null) levelManagerObj = new GameObject("LevelManager");
            LevelManager levelManager = GetOrAddComponent<LevelManager>(levelManagerObj);

            GameObject scoreManagerObj = GameObject.Find("ScoreManager");
            if (scoreManagerObj == null) scoreManagerObj = new GameObject("ScoreManager");
            ScoreManager scoreManager = GetOrAddComponent<ScoreManager>(scoreManagerObj);

            // 5. BubbleGrid
            GameObject gridObj = GameObject.Find("BubbleGrid");
            if (gridObj == null) gridObj = new GameObject("BubbleGrid");
            BubbleGrid bubbleGrid = GetOrAddComponent<BubbleGrid>(gridObj);
            SetPrivateField(bubbleGrid, "maxRows", 14);
            SetPrivateField(bubbleGrid, "maxColumns", 8);
            SetPrivateField(bubbleGrid, "bubbleDiameter", 0.8f);
            SetPrivateField(bubbleGrid, "gridOrigin", new Vector2(-2.8f, 3.5f));
            bubbleGrid.InitializeGrid(14, 8);

            for (int r = 0; r < 5; r++)
            {
                string rowName = $"Row_{r:D2}";
                if (gridObj.transform.Find(rowName) == null)
                {
                    GameObject rowObj = new GameObject(rowName);
                    rowObj.transform.SetParent(gridObj.transform, false);
                }
            }

            // 6. InitialBubbles Hierarchy
            GameObject initialBubblesObj = GameObject.Find("InitialBubbles");
            if (initialBubblesObj == null)
            {
                initialBubblesObj = new GameObject("InitialBubbles");

                BubbleColor[,] pattern = new BubbleColor[5, 8]
                {
                    { BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Blue,   BubbleColor.Blue,   BubbleColor.Yellow, BubbleColor.Yellow },
                    { BubbleColor.Blue,   BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Green },
                    { BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Blue },
                    { BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Blue,   BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Red },
                    { BubbleColor.Green,  BubbleColor.Blue,   BubbleColor.Yellow, BubbleColor.Purple, BubbleColor.Purple, BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Green }
                };

                int bubbleIndex = 0;
                for (int r = 0; r < 5; r++)
                {
                    int cols = (r % 2 != 0) ? 7 : 8;
                    for (int c = 0; c < cols; c++)
                    {
                        GameObject bubbleObj = Instantiate(bubblePrefab, initialBubblesObj.transform);
                        bubbleObj.name = $"Bubble_{bubbleIndex:D2}";
                        BubbleController controller = bubbleObj.GetComponent<BubbleController>();
                        BubbleColor color = pattern[r, c % 8];

                        controller.SetColor(color, circleSprite);
                        controller.SetGridPosition(r, c);
                        bubbleObj.transform.position = bubbleGrid.GridToWorldPosition(r, c);
                        controller.Attach();

                        bubbleGrid.AddBubble(r, c, controller);
                        bubbleIndex++;
                    }
                }
            }

            // 7. Shooter Hierarchy
            GameObject shooterObj = GameObject.Find("Shooter");
            if (shooterObj == null) shooterObj = new GameObject("Shooter");
            ShooterController shooterController = GetOrAddComponent<ShooterController>(shooterObj);
            AimController aimController = GetOrAddComponent<AimController>(shooterObj);

            GameObject shooterBase = GetOrCreateChild(shooterObj, "ShooterBase");
            shooterBase.transform.position = new Vector3(0f, -3.8f, 0f);
            SpriteRenderer baseSr = GetOrAddComponent<SpriteRenderer>(shooterBase);
            baseSr.sprite = circleSprite;
            baseSr.color = new Color(0.35f, 0.4f, 0.5f, 1f);
            shooterBase.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            GameObject currentBubbleObj = GetOrCreateChild(shooterObj, "CurrentBubble");
            currentBubbleObj.transform.position = new Vector3(0f, -3.5f, 0f);
            SpriteRenderer curSr = GetOrAddComponent<SpriteRenderer>(currentBubbleObj);
            curSr.sprite = circleSprite;
            curSr.color = BubbleColorUtility.GetColorValue(BubbleColor.Red);
            GetOrAddComponent<CircleCollider2D>(currentBubbleObj).radius = 0.45f;
            BubbleController currentController = GetOrAddComponent<BubbleController>(currentBubbleObj);
            currentController.SetColor(BubbleColor.Red, circleSprite);

            GameObject aimPoint = GetOrCreateChild(shooterObj, "AimPoint");
            aimPoint.transform.position = new Vector3(0f, -3.5f, 0f);

            GameObject aimLineObj = GetOrCreateChild(shooterObj, "AimLine");
            LineRenderer aimLine = GetOrAddComponent<LineRenderer>(aimLineObj);
            aimLine.startWidth = 0.08f;
            aimLine.endWidth = 0.04f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startColor = new Color(1f, 1f, 1f, 0.8f);
            aimLine.endColor = new Color(1f, 1f, 1f, 0.3f);
            aimLine.positionCount = 0;

            // 8. GameplayBounds Hierarchy
            GameObject boundsObj = GameObject.Find("GameplayBounds");
            if (boundsObj == null)
            {
                boundsObj = new GameObject("GameplayBounds");
                CreateWall(boundsObj, "LeftWall", new Vector3(-3.4f, 0f, 0f), new Vector3(0.4f, 10f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.2f));
                CreateWall(boundsObj, "RightWall", new Vector3(3.4f, 0f, 0f), new Vector3(0.4f, 10f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.2f));
                CreateWall(boundsObj, "Ceiling", new Vector3(0f, 4.2f, 0f), new Vector3(7.2f, 0.4f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.3f));
                CreateWall(boundsObj, "LoseLine", new Vector3(0f, -2.8f, 0f), new Vector3(7f, 0.05f, 1f), squareSprite, new Color(1f, 0.3f, 0.3f, 0.5f));
            }

            // 9. Effects Hierarchy
            GameObject effectsObj = GameObject.Find("Effects");
            if (effectsObj == null)
            {
                effectsObj = new GameObject("Effects");
                GetOrCreateChild(effectsObj, "PopEffects");
                GetOrCreateChild(effectsObj, "FallEffects");
            }

            // 10. GameplayCanvas Hierarchy & UI
            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameplayUI gameplayUI = GetOrAddComponent<GameplayUI>(canvasObj);

            // TopBar
            GameObject topBar = GetOrCreateChild(canvasObj, "TopBar");
            RectTransform topRect = GetOrAddComponent<RectTransform>(topBar);
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0, 160);
            topRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI levelText = GetOrCreateUIText(topBar, "LevelText", "LEVEL 1", 42, TextAlignmentOptions.Left);
            RectTransform lvlRect = levelText.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.05f, 0.3f);
            lvlRect.anchorMax = new Vector2(0.35f, 0.7f);
            lvlRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI scoreText = GetOrCreateUIText(topBar, "ScoreText", "SCORE: 0", 42, TextAlignmentOptions.Right);
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.4f, 0.3f);
            scoreRect.anchorMax = new Vector2(0.8f, 0.7f);
            scoreRect.anchoredPosition = Vector2.zero;

            Button pauseBtn = GetOrCreateUIButton(topBar, "PauseButton", "||", 36, squareSprite);
            RectTransform pauseRect = pauseBtn.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(0.9f, 0.5f);
            pauseRect.anchorMax = new Vector2(0.9f, 0.5f);
            pauseRect.sizeDelta = new Vector2(90, 90);
            pauseRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI nextLabel = GetOrCreateUIText(canvasObj, "NextBubbleLabel", "NEXT", 28, TextAlignmentOptions.Center);
            RectTransform nextLabelRect = nextLabel.GetComponent<RectTransform>();
            nextLabelRect.anchorMin = new Vector2(0.1f, 0.04f);
            nextLabelRect.anchorMax = new Vector2(0.3f, 0.08f);

            // NextBubbleArea UI & Preview Bubble
            GameObject nextArea = GetOrCreateChild(canvasObj, "NextBubbleArea");
            RectTransform nextAreaRect = GetOrAddComponent<RectTransform>(nextArea);
            nextAreaRect.anchorMin = new Vector2(0.12f, 0.08f);
            nextAreaRect.anchorMax = new Vector2(0.28f, 0.16f);

            GameObject nextPreviewObj = GetOrCreateChild(shooterObj, "NextBubble");
            nextPreviewObj.transform.position = new Vector3(-2.2f, -4.1f, 0f);
            nextPreviewObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            SpriteRenderer nextSr = GetOrAddComponent<SpriteRenderer>(nextPreviewObj);
            nextSr.sprite = circleSprite;
            nextSr.color = BubbleColorUtility.GetColorValue(BubbleColor.Blue);
            GetOrAddComponent<CircleCollider2D>(nextPreviewObj).radius = 0.45f;
            BubbleController nextController = GetOrAddComponent<BubbleController>(nextPreviewObj);
            nextController.SetColor(BubbleColor.Blue, circleSprite);

            // InstructionText
            TextMeshProUGUI instructionText = GetOrCreateUIText(canvasObj, "InstructionText", "AIM AND SHOOT", 34, TextAlignmentOptions.Center);
            RectTransform instRect = instructionText.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(0.1f, 0.18f);
            instRect.anchorMax = new Vector2(0.9f, 0.24f);

            // WinPanel UI
            GameObject winPanelObj = GetOrCreateUIPanel(canvasObj, "WinPanel", squareSprite, new Color(0.1f, 0.15f, 0.22f, 0.96f));
            TextMeshProUGUI winTitle = GetOrCreateUIText(winPanelObj, "WinTitle", "LEVEL COMPLETE", 54, TextAlignmentOptions.Center);
            winTitle.rectTransform.anchoredPosition = new Vector2(0, 220);
            TextMeshProUGUI winScore = GetOrCreateUIText(winPanelObj, "WinScoreText", "FINAL SCORE: 0", 40, TextAlignmentOptions.Center);
            winScore.rectTransform.anchoredPosition = new Vector2(0, 100);

            GameObject winStars = GetOrCreateChild(winPanelObj, "WinStars");
            winStars.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Button winNextBtn = GetOrCreateUIButton(winPanelObj, "NextButton", "NEXT LEVEL", 36, squareSprite);
            winNextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            Button winRetryBtn = GetOrCreateUIButton(winPanelObj, "RetryButton", "RETRY", 36, squareSprite);
            winRetryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -220);
            Button winHomeBtn = GetOrCreateUIButton(winPanelObj, "HomeButton", "HOME", 36, squareSprite);
            winHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, -220);
            winPanelObj.SetActive(false);

            // LosePanel UI
            GameObject losePanelObj = GetOrCreateUIPanel(canvasObj, "LosePanel", squareSprite, new Color(0.25f, 0.1f, 0.12f, 0.96f));
            TextMeshProUGUI loseTitle = GetOrCreateUIText(losePanelObj, "LoseTitle", "LEVEL FAILED", 54, TextAlignmentOptions.Center);
            loseTitle.rectTransform.anchoredPosition = new Vector2(0, 200);
            TextMeshProUGUI loseScore = GetOrCreateUIText(losePanelObj, "LoseScoreText", "FINAL SCORE: 0", 40, TextAlignmentOptions.Center);
            loseScore.rectTransform.anchoredPosition = new Vector2(0, 80);
            Button loseRetryBtn = GetOrCreateUIButton(losePanelObj, "RetryButton", "RETRY", 36, squareSprite);
            loseRetryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, -100);
            Button loseHomeBtn = GetOrCreateUIButton(losePanelObj, "HomeButton", "HOME", 36, squareSprite);
            loseHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(130, -100);
            losePanelObj.SetActive(false);

            // PausePanel UI
            GameObject pausePanelObj = GetOrCreateUIPanel(canvasObj, "PausePanel", squareSprite, new Color(0.12f, 0.12f, 0.18f, 0.96f));
            TextMeshProUGUI pauseTitle = GetOrCreateUIText(pausePanelObj, "PauseTitle", "PAUSED", 54, TextAlignmentOptions.Center);
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0, 220);
            Button pauseResumeBtn = GetOrCreateUIButton(pausePanelObj, "ResumeButton", "RESUME", 36, squareSprite);
            pauseResumeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);
            Button pauseRestartBtn = GetOrCreateUIButton(pausePanelObj, "RestartButton", "RESTART", 36, squareSprite);
            pauseRestartBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            Button pauseHomeBtn = GetOrCreateUIButton(pausePanelObj, "HomeButton", "HOME", 36, squareSprite);
            pauseHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -170);
            pausePanelObj.SetActive(false);

            // Wire Serialized References
            SetPrivateField(gameplayUI, "levelText", levelText);
            SetPrivateField(gameplayUI, "scoreText", scoreText);
            SetPrivateField(gameplayUI, "pauseButton", pauseBtn);
            SetPrivateField(gameplayUI, "instructionText", instructionText);

            SetPrivateField(gameplayUI, "winPanel", winPanelObj);
            SetPrivateField(gameplayUI, "winScoreText", winScore);
            SetPrivateField(gameplayUI, "winNextButton", winNextBtn);
            SetPrivateField(gameplayUI, "winRetryButton", winRetryBtn);
            SetPrivateField(gameplayUI, "winHomeButton", winHomeBtn);

            SetPrivateField(gameplayUI, "losePanel", losePanelObj);
            SetPrivateField(gameplayUI, "loseScoreText", loseScore);
            SetPrivateField(gameplayUI, "loseRetryButton", loseRetryBtn);
            SetPrivateField(gameplayUI, "loseHomeButton", loseHomeBtn);

            SetPrivateField(gameplayUI, "pausePanel", pausePanelObj);
            SetPrivateField(gameplayUI, "pauseResumeButton", pauseResumeBtn);
            SetPrivateField(gameplayUI, "pauseRestartButton", pauseRestartBtn);
            SetPrivateField(gameplayUI, "pauseHomeButton", pauseHomeBtn);

            SetPrivateField(shooterController, "currentBubblePosition", currentBubbleObj.transform);
            SetPrivateField(shooterController, "nextBubblePosition", nextPreviewObj.transform);
            SetPrivateField(shooterController, "shootingSpeed", 14f);
            SetPrivateField(shooterController, "bubbleGrid", bubbleGrid);
            SetPrivateField(shooterController, "bubblePrefab", bubblePrefab);
            SetPrivateField(shooterController, "colorGenerator", colorGenerator);

            SetPrivateField(aimController, "lineRenderer", aimLine);
            SetPrivateField(aimController, "mainCamera", mainCam);

            SetPrivateField(levelManager, "bubbleGrid", bubbleGrid);
            SetPrivateField(levelManager, "bubblePrefab", bubblePrefab);

            SetPrivateField(colorGenerator, "bubbleGrid", bubbleGrid);
            SetPrivateField(matchFinder, "bubbleGrid", bubbleGrid);
            SetPrivateField(floatingDetector, "bubbleGrid", bubbleGrid);

            SetPrivateField(gameManager, "bubbleGrid", bubbleGrid);
            SetPrivateField(gameManager, "levelManager", levelManager);
            SetPrivateField(gameManager, "shooterController", shooterController);
            SetPrivateField(gameManager, "aimController", aimController);
            SetPrivateField(gameManager, "matchFinder", matchFinder);
            SetPrivateField(gameManager, "floatingDetector", floatingDetector);
            SetPrivateField(gameManager, "scoreManager", scoreManager);
            SetPrivateField(gameManager, "gameplayUI", gameplayUI);
            SetPrivateField(gameManager, "colorGenerator", colorGenerator);
            SetPrivateField(gameManager, "bubblePrefab", bubblePrefab);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }
#endif

            Debug.Log("[BubbleShooter] Physical scene hierarchy built & saved successfully!");
        }

        private Sprite CreateCircleSprite()
        {
            int res = 64;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float center = res * 0.5f;
            float radius = res * 0.46f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(0, 1, Color.white);
            tex.SetPixel(1, 0, Color.white);
            tex.SetPixel(1, 1, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
        }

        private GameObject CreateBubblePrefabObject(Sprite circleSprite)
        {
            GameObject prefab = new GameObject("BubblePrefab");
            SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            CircleCollider2D col = prefab.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            prefab.AddComponent<BubbleController>();
            prefab.transform.localScale = Vector3.one * 0.8f;
            prefab.SetActive(false);
            return prefab;
        }

        private T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T comp = target.GetComponent<T>();
            if (comp == null) comp = target.AddComponent<T>();
            return comp;
        }

        private GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            Transform t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.AddComponent<RectTransform>();
            return child;
        }

        private TextMeshProUGUI GetOrCreateUIText(GameObject parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject child = GetOrCreateChild(parent, name);
            TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(child);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private Button GetOrCreateUIButton(GameObject parent, string name, string label, float fontSize, Sprite sprite)
        {
            GameObject child = GetOrCreateChild(parent, name);
            Image img = GetOrAddComponent<Image>(child);
            img.sprite = sprite;
            img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            Button btn = GetOrAddComponent<Button>(child);

            RectTransform rect = GetOrAddComponent<RectTransform>(child);
            rect.sizeDelta = new Vector2(260, 85);

            TextMeshProUGUI text = GetOrCreateUIText(child, "Text", label, fontSize, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btn;
        }

        private GameObject GetOrCreateUIPanel(GameObject parent, string name, Sprite sprite, Color bgColor)
        {
            GameObject panel = GetOrCreateChild(parent, name);
            Image img = GetOrAddComponent<Image>(panel);
            img.sprite = sprite;
            img.color = bgColor;
            RectTransform rect = GetOrAddComponent<RectTransform>(panel);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return panel;
        }

        private void CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite, Color col)
        {
            GameObject wall = GetOrCreateChild(parent, name);
            wall.transform.position = pos;
            wall.transform.localScale = scale;

            GetOrAddComponent<BoxCollider2D>(wall);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(wall);
            sr.sprite = sprite;
            sr.color = col;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
