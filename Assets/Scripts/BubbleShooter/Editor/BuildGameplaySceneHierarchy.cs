using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BubbleShooter.Core;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.Shooter;
using BubbleShooter.Matching;
using BubbleShooter.Level;
using BubbleShooter.Score;
using BubbleShooter.UI;

namespace BubbleShooter.Editor
{
    [InitializeOnLoad]
    public class BuildGameplaySceneHierarchy
    {
        static BuildGameplaySceneHierarchy()
        {
            // Constructor called on Editor load
        }

        [MenuItem("BubbleShooter/Build Complete Gameplay Scene Hierarchy")]
        public static void BuildScene()
        {
            Debug.Log("[BubbleShooter] Starting Physical Gameplay Scene Hierarchy Generation...");

            EnsureDirectories();
            Sprite circleSprite = CreateCircleSpriteAsset();
            Sprite squareSprite = CreateSquareSpriteAsset();
            GameObject bubblePrefab = CreateBubblePrefab(circleSprite);
            BubbleLevelData level1Data = CreateLevel1DataAsset();

            // Open GamePlay scene
            string scenePath = "Assets/Scenes/GamePlay.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Clear existing scene roots except Main Camera
            GameObject[] rootObjects = scene.GetRootGameObjects();
            Camera mainCam = null;

            foreach (var root in rootObjects)
            {
                if (root.name == "Main Camera" || root.GetComponent<Camera>() != null)
                {
                    mainCam = root.GetComponent<Camera>();
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }

            // 1. Configure Main Camera
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.0f;
            mainCam.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.transform.position = new Vector3(0, 0, -10f);

            // 2. GameplayBackground
            GameObject bgObj = new GameObject("GameplayBackground");
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = squareSprite;
            bgSr.color = new Color(0.1f, 0.13f, 0.2f, 1f);
            bgSr.sortingOrder = -10;
            bgObj.transform.position = new Vector3(0, 0, 5f);
            bgObj.transform.localScale = new Vector3(15f, 20f, 1f);

            // 3. Manager Objects
            GameObject managerObj = new GameObject("GameManager");
            BubbleShooterGameManager gameManager = managerObj.AddComponent<BubbleShooterGameManager>();
            BubbleColorGenerator colorGenerator = managerObj.AddComponent<BubbleColorGenerator>();
            BubbleMatchFinder matchFinder = managerObj.AddComponent<BubbleMatchFinder>();
            FloatingBubbleDetector floatingDetector = managerObj.AddComponent<FloatingBubbleDetector>();

            GameObject levelManagerObj = new GameObject("LevelManager");
            LevelManager levelManager = levelManagerObj.AddComponent<LevelManager>();

            GameObject scoreManagerObj = new GameObject("ScoreManager");
            ScoreManager scoreManager = scoreManagerObj.AddComponent<ScoreManager>();

            // 4. BubbleGrid & Row hierarchy
            GameObject gridObj = new GameObject("BubbleGrid");
            BubbleGrid bubbleGrid = gridObj.AddComponent<BubbleGrid>();
            SetPrivateField(bubbleGrid, "maxRows", 14);
            SetPrivateField(bubbleGrid, "maxColumns", 8);
            SetPrivateField(bubbleGrid, "bubbleDiameter", 0.8f);
            SetPrivateField(bubbleGrid, "gridOrigin", new Vector2(-2.8f, 3.5f));
            bubbleGrid.InitializeGrid(14, 8);

            for (int r = 0; r < 5; r++)
            {
                GameObject rowObj = new GameObject($"Row_{r:D2}");
                rowObj.transform.SetParent(gridObj.transform, false);
            }

            // 5. InitialBubbles Hierarchy
            //GameObject initialBubblesObj = new GameObject("InitialBubbles");

            //BubbleColor[,] pattern = new BubbleColor[5, 8]
            //{
            //    { BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Blue,   BubbleColor.Blue,   BubbleColor.Yellow, BubbleColor.Yellow },
            //    { BubbleColor.Blue,   BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Green },
            //    { BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Blue },
            //    { BubbleColor.Yellow, BubbleColor.Yellow, BubbleColor.Blue,   BubbleColor.Red,    BubbleColor.Red,    BubbleColor.Green,  BubbleColor.Green,  BubbleColor.Red },
            //    { BubbleColor.Green,  BubbleColor.Blue,   BubbleColor.Yellow, BubbleColor.Purple, BubbleColor.Purple, BubbleColor.Red,    BubbleColor.Blue,   BubbleColor.Green }
            //};

            //int bubbleIndex = 0;
            //for (int r = 0; r < 5; r++)
            //{
            //    int cols = (r % 2 != 0) ? 7 : 8;
            //    for (int c = 0; c < cols; c++)
            //    {
            //        GameObject bubbleObj = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, initialBubblesObj.transform);
            //        bubbleObj.name = $"Bubble_{bubbleIndex:D2}";
            //        BubbleController controller = bubbleObj.GetComponent<BubbleController>();
            //        BubbleColor color = pattern[r, c % 8];

            //        controller.SetColor(color, circleSprite);
            //        controller.SetGridPosition(r, c);
            //        bubbleObj.transform.position = bubbleGrid.GridToWorldPosition(r, c);
            //        controller.Attach();

            //        bubbleGrid.AddBubble(r, c, controller);
            //        bubbleIndex++;
            //    }
            //}

            // 6. Shooter Hierarchy
            GameObject shooterObj = new GameObject("Shooter");
            ShooterController shooterController = shooterObj.AddComponent<ShooterController>();
            AimController aimController = shooterObj.AddComponent<AimController>();

            GameObject shooterBase = new GameObject("ShooterBase");
            shooterBase.transform.SetParent(shooterObj.transform, false);
            shooterBase.transform.position = new Vector3(0f, -3.8f, 0f);
            SpriteRenderer baseSr = shooterBase.AddComponent<SpriteRenderer>();
            baseSr.sprite = circleSprite;
            baseSr.color = new Color(0.35f, 0.4f, 0.5f, 1f);
            shooterBase.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            GameObject currentBubbleObj = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, shooterObj.transform);
            currentBubbleObj.name = "CurrentBubble";
            currentBubbleObj.transform.position = new Vector3(0f, -3.5f, 0f);
            BubbleController currentController = currentBubbleObj.GetComponent<BubbleController>();
            currentController.SetColor(BubbleColor.Red, circleSprite);

            GameObject aimPoint = new GameObject("AimPoint");
            aimPoint.transform.SetParent(shooterObj.transform, false);
            aimPoint.transform.position = new Vector3(0f, -3.5f, 0f);

            GameObject aimLineObj = new GameObject("AimLine");
            aimLineObj.transform.SetParent(shooterObj.transform, false);
            LineRenderer aimLine = aimLineObj.AddComponent<LineRenderer>();
            aimLine.startWidth = 0.08f;
            aimLine.endWidth = 0.04f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startColor = new Color(1f, 1f, 1f, 0.8f);
            aimLine.endColor = new Color(1f, 1f, 1f, 0.3f);
            aimLine.positionCount = 0;

            // 7. GameplayBounds Hierarchy
            GameObject boundsObj = new GameObject("GameplayBounds");
            CreateWall(boundsObj, "LeftWall", new Vector3(-3.4f, 0f, 0f), new Vector3(0.4f, 10f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.2f));
            CreateWall(boundsObj, "RightWall", new Vector3(3.4f, 0f, 0f), new Vector3(0.4f, 10f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.2f));
            CreateWall(boundsObj, "Ceiling", new Vector3(0f, 4.2f, 0f), new Vector3(7.2f, 0.4f, 1f), squareSprite, new Color(0.3f, 0.4f, 0.5f, 0.3f));
            CreateWall(boundsObj, "LoseLine", new Vector3(0f, -2.8f, 0f), new Vector3(7f, 0.05f, 1f), squareSprite, new Color(1f, 0.3f, 0.3f, 0.5f));

            // 8. Effects Hierarchy
            GameObject effectsObj = new GameObject("Effects");
            GameObject popEffects = new GameObject("PopEffects");
            popEffects.transform.SetParent(effectsObj.transform, false);
            GameObject fallEffects = new GameObject("FallEffects");
            fallEffects.transform.SetParent(effectsObj.transform, false);

            // 9. GameplayCanvas Hierarchy & UI
            GameObject canvasObj = new GameObject("GameplayCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            GameplayUI gameplayUI = canvasObj.AddComponent<GameplayUI>();

            // TopBar UI
            GameObject topBar = CreateUIElement("TopBar", canvasObj.transform);
            RectTransform topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0, 160);
            topRect.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI levelText = CreateUIText("LevelText", topBar.transform, "LEVEL 1", 42, TextAlignmentOptions.Left);
            RectTransform lvlRect = levelText.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.05f, 0.3f);
            lvlRect.anchorMax = new Vector2(0.35f, 0.7f);
            lvlRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI scoreText = CreateUIText("ScoreText", topBar.transform, "SCORE: 0", 42, TextAlignmentOptions.Right);
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.4f, 0.3f);
            scoreRect.anchorMax = new Vector2(0.8f, 0.7f);
            scoreRect.anchoredPosition = Vector2.zero;

            Button pauseBtn = CreateUIButton("PauseButton", topBar.transform, "||", 36, squareSprite);
            RectTransform pauseRect = pauseBtn.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(0.9f, 0.5f);
            pauseRect.anchorMax = new Vector2(0.9f, 0.5f);
            pauseRect.sizeDelta = new Vector2(90, 90);
            pauseRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI nextLabel = CreateUIText("NextBubbleLabel", canvasObj.transform, "NEXT", 28, TextAlignmentOptions.Center);
            RectTransform nextLabelRect = nextLabel.GetComponent<RectTransform>();
            nextLabelRect.anchorMin = new Vector2(0.1f, 0.04f);
            nextLabelRect.anchorMax = new Vector2(0.3f, 0.08f);

            // NextBubbleArea & NextBubblePreview UI
            GameObject nextArea = CreateUIElement("NextBubbleArea", canvasObj.transform);
            RectTransform nextAreaRect = nextArea.GetComponent<RectTransform>();
            nextAreaRect.anchorMin = new Vector2(0.12f, 0.08f);
            nextAreaRect.anchorMax = new Vector2(0.28f, 0.16f);

            GameObject nextPreviewObj = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, shooterObj.transform);
            nextPreviewObj.name = "NextBubble";
            nextPreviewObj.transform.position = new Vector3(-2.2f, -4.1f, 0f);
            nextPreviewObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            BubbleController nextController = nextPreviewObj.GetComponent<BubbleController>();
            nextController.SetColor(BubbleColor.Blue, circleSprite);

            // InstructionText UI
            TextMeshProUGUI instructionText = CreateUIText("InstructionText", canvasObj.transform, "AIM AND SHOOT", 34, TextAlignmentOptions.Center);
            RectTransform instRect = instructionText.GetComponent<RectTransform>();
            instRect.anchorMin = new Vector2(0.1f, 0.18f);
            instRect.anchorMax = new Vector2(0.9f, 0.24f);

            // WinPanel UI
            GameObject winPanelObj = CreateUIPanel("WinPanel", canvasObj.transform, squareSprite, new Color(0.1f, 0.15f, 0.22f, 0.96f));
            TextMeshProUGUI winTitle = CreateUIText("WinTitle", winPanelObj.transform, "LEVEL COMPLETE", 54, TextAlignmentOptions.Center);
            winTitle.rectTransform.anchoredPosition = new Vector2(0, 220);
            TextMeshProUGUI winScore = CreateUIText("WinScoreText", winPanelObj.transform, "FINAL SCORE: 0", 40, TextAlignmentOptions.Center);
            winScore.rectTransform.anchoredPosition = new Vector2(0, 100);

            GameObject winStars = CreateUIElement("WinStars", winPanelObj.transform);
            winStars.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

            Button winNextBtn = CreateUIButton("NextButton", winPanelObj.transform, "NEXT LEVEL", 36, squareSprite);
            winNextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            Button winRetryBtn = CreateUIButton("RetryButton", winPanelObj.transform, "RETRY", 36, squareSprite);
            winRetryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -220);
            Button winHomeBtn = CreateUIButton("HomeButton", winPanelObj.transform, "HOME", 36, squareSprite);
            winHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, -220);
            winPanelObj.SetActive(false);

            // LosePanel UI
            GameObject losePanelObj = CreateUIPanel("LosePanel", canvasObj.transform, squareSprite, new Color(0.25f, 0.1f, 0.12f, 0.96f));
            TextMeshProUGUI loseTitle = CreateUIText("LoseTitle", losePanelObj.transform, "LEVEL FAILED", 54, TextAlignmentOptions.Center);
            loseTitle.rectTransform.anchoredPosition = new Vector2(0, 200);
            TextMeshProUGUI loseScore = CreateUIText("LoseScoreText", losePanelObj.transform, "FINAL SCORE: 0", 40, TextAlignmentOptions.Center);
            loseScore.rectTransform.anchoredPosition = new Vector2(0, 80);
            Button loseRetryBtn = CreateUIButton("RetryButton", losePanelObj.transform, "RETRY", 36, squareSprite);
            loseRetryBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, -100);
            Button loseHomeBtn = CreateUIButton("HomeButton", losePanelObj.transform, "HOME", 36, squareSprite);
            loseHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(130, -100);
            losePanelObj.SetActive(false);

            // PausePanel UI
            GameObject pausePanelObj = CreateUIPanel("PausePanel", canvasObj.transform, squareSprite, new Color(0.12f, 0.12f, 0.18f, 0.96f));
            TextMeshProUGUI pauseTitle = CreateUIText("PauseTitle", pausePanelObj.transform, "PAUSED", 54, TextAlignmentOptions.Center);
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0, 220);
            Button pauseResumeBtn = CreateUIButton("ResumeButton", pausePanelObj.transform, "RESUME", 36, squareSprite);
            pauseResumeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);
            Button pauseRestartBtn = CreateUIButton("RestartButton", pausePanelObj.transform, "RESTART", 36, squareSprite);
            pauseRestartBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            Button pauseHomeBtn = CreateUIButton("HomeButton", pausePanelObj.transform, "HOME", 36, squareSprite);
            pauseHomeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -170);
            pausePanelObj.SetActive(false);

            // Assign Inspector Serialized References across components
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
            SetPrivateField(levelManager, "currentLevelData", level1Data);

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

            // Mark dirty & save scene
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();

            File.WriteAllText("scene_built.txt", $"BUILD_SUCCESS_SAVED_{saved}");
            Debug.Log($"[BubbleShooter] COMPLETE GAMEPLAY SCENE HIERARCHY BUILT AND SAVED! Result: {saved}");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Sprites")) Directory.CreateDirectory("Assets/Sprites");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
        }

        private static Sprite CreateCircleSpriteAsset()
        {
            string path = "Assets/Sprites/BubbleSprite.png";
            if (!File.Exists(path))
            {
                int res = 128;
                Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
                float center = res * 0.5f;
                float radius = res * 0.47f;

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
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateSquareSpriteAsset()
        {
            string path = "Assets/Sprites/SquareSprite.png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateBubblePrefab(Sprite circleSprite)
        {
            string path = "Assets/Prefabs/BubblePrefab.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = new GameObject("BubblePrefab");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            temp.AddComponent<BubbleController>();
            temp.transform.localScale = Vector3.one * 0.8f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }


        private static BubbleLevelData CreateLevel1DataAsset()
        {
            string path = "Assets/Resources/Level1Data.asset";

            BubbleLevelData data =
                AssetDatabase.LoadAssetAtPath<BubbleLevelData>(path);

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BubbleLevelData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.levelNumber = 1;
            data.rows = 4;
            data.columns = 4;
            data.minimumMatchCount = 3;
            data.maximumMissedShots = 5;

            // Keep the existing miss-row system disabled for now.
            data.enableMissRows = false;
            data.bubbleSpeed = 14f;
            data.dangerRow = 10;

            data.availableColors = new List<BubbleColor>
    {
        BubbleColor.Red,
        BubbleColor.Blue,
        BubbleColor.Green
    };

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            return data;
        }

        private static void CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite, Color col)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent.transform, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();

            SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = col;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static TextMeshProUGUI CreateUIText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateUIButton(string name, Transform parent, string label, float fontSize, Sprite sprite)
        {
            GameObject obj = CreateUIElement(name, parent);
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            Button btn = obj.AddComponent<Button>();

            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            colors.pressedColor = new Color(0.15f, 0.45f, 0.75f, 1f);
            btn.colors = colors;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260, 85);

            TextMeshProUGUI text = CreateUIText("Text", obj.transform, label, fontSize, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btn;
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Sprite sprite, Color bgColor)
        {
            GameObject panel = CreateUIElement(name, parent);
            Image img = panel.AddComponent<Image>();
            img.sprite = sprite;
            img.color = bgColor;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return panel;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
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
