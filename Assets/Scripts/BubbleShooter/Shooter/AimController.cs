
using System;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Core;
using BubbleShooter.Grid;
using BubbleShooter.Bubble;
using BubbleShooter.FX;

namespace BubbleShooter.Shooter
{
    public class AimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Camera mainCamera;

        [Tooltip("Bubble grid for wall, ceiling and bubble collision.")]
        [SerializeField] private BubbleGrid grid;

        [Tooltip("Optional transparent bubble showing the landing cell.")]
        [SerializeField] private SpriteRenderer ghostBubble;

        [Header("Aim Config")]
        [SerializeField] private float minAimAngle = 15f;
        [SerializeField] private float maxAimAngle = 165f;

        [Header("Trajectory")]
        [SerializeField] private float maxTraceLength = 40f;
        [SerializeField] private int maxBounces = 10;

        [Header("Line Look")]
        [SerializeField] private float startWidth = 0.08f;
        [SerializeField] private float endWidth = 0.08f;
        [SerializeField]
        private Color startColor =
            new Color(1f, 1f, 1f, 0.85f);
        [SerializeField]
        private Color endColor =
            new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private bool dotted = false;
        [SerializeField] private float dashPeriod = 0.3f;
        [SerializeField] private int sortingOrder = 50;

        [Header("Dotted Aim Preview")]
        [Tooltip("Enable the dotted trajectory preview.")]
        [SerializeField] private bool useDottedPreview = true;

        [Tooltip("Assign a Circle sprite prefab with a SpriteRenderer.")]
        [SerializeField] private Transform dotPrefab;

        [SerializeField] private float dotSpacing = 0.18f;
        [SerializeField] private float dotSize = 0.07f;

        [SerializeField]
        private Color dotColor = new Color(0.15f, 0.9f, 1f, 0.95f);

        [SerializeField] private int maxDots = 100;

        [Header("Landing Marker")]
        [SerializeField] private bool showLandingMarker = true;
        [SerializeField] private float landingMarkerSize = 0.16f;
        [SerializeField] private Color landingMarkerColor = Color.white;

        [Header("Fallback Bounds")]
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float topCeilingY = 3.8f;

        private Vector2 currentAimDirection = Vector2.up;
        private bool isAiming = false;

        private readonly List<Vector3> trajectoryPoints =
            new List<Vector3>(16);

        private readonly List<Transform> aimDots =
            new List<Transform>();

        private Transform landingMarker;

        // Runtime visuals (jab Inspector mein dotPrefab / ghostBubble na diye hon)
        private Transform runtimeDotTemplate;
        private bool usingRuntimeDots;
        private SpriteRenderer runtimeGhostRing;
        private bool boundsSet;
        private bool pointerBlocked;
        private Color baseAimColor = Color.white;
        private float marchOffset;

        public Vector2 CurrentAimDirection => currentAimDirection;
        public bool IsAiming => isAiming;

        public IReadOnlyList<Vector3> TrajectoryPoints =>
            trajectoryPoints;

        public Vector3 TrajectoryEnd { get; private set; }

        public event Action<Vector2> OnAimReleased;

        // IMPORTANT: SetBounds() wali hadein wahi hain jo asli projectile use karti hai.
        // Pehle preview grid.LeftWallX par takrata tha magar bubble aadha diameter aage ja kar,
        // is liye dikhaya gaya raasta aur asli raasta alag hote the.
        private float LeftX =>
            boundsSet ? leftWallX : (grid != null ? grid.LeftWallX : leftWallX);

        private float RightX =>
            boundsSet ? rightWallX : (grid != null ? grid.RightWallX : rightWallX);

        private float CeilingY =>
            boundsSet ? topCeilingY : (grid != null ? grid.CeilingY : topCeilingY);

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (grid == null)
                grid = FindObjectOfType<BubbleGrid>();

            SetupLineRenderer();
            SetupRuntimeVisuals();
            HideAimLine();
        }

        /// <summary>
        /// dotPrefab / ghostBubble khali hon to code se bana lo: chamakte gol nuqte
        /// aur landing jagah par shaffaf "ghost" bubble.
        /// </summary>
        private void SetupRuntimeVisuals()
        {
            if (dotPrefab == null)
            {
                GameObject t = new GameObject("AimDotTemplate");
                t.transform.SetParent(transform, false);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = FxSprites.Circle;
                t.SetActive(false);
                runtimeDotTemplate = t.transform;
                dotPrefab = runtimeDotTemplate;
                usingRuntimeDots = true;
                showLandingMarker = false;   // ghost bubble kaafi hai
                useDottedPreview = true;
            }

            if (ghostBubble == null)
            {
                GameObject g = new GameObject("AimGhostBubble");
                g.transform.SetParent(transform, false);
                ghostBubble = g.AddComponent<SpriteRenderer>();
                ghostBubble.sprite = BubbleSpriteFactory.GetBaseSprite();
                ghostBubble.sortingOrder = sortingOrder - 1;
                ghostBubble.enabled = false;

                GameObject r = new GameObject("GhostRing");
                r.transform.SetParent(g.transform, false);
                runtimeGhostRing = r.AddComponent<SpriteRenderer>();
                runtimeGhostRing.sprite = FxSprites.Ring;
                runtimeGhostRing.sortingOrder = sortingOrder;
                runtimeGhostRing.enabled = false;
            }
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.sortingOrder = sortingOrder;

            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning(
                    "[AimController] Sprites/Default shader not found.");
                return;
            }

            Material mat = new Material(shader);

            if (dotted)
            {
                Texture2D tex = new Texture2D(
                    2, 1, TextureFormat.RGBA32, false);

                tex.SetPixel(0, 0, Color.white);
                tex.SetPixel(1, 0, new Color(1f, 1f, 1f, 0f));
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.Apply();

                mat.mainTexture = tex;
                mat.mainTextureScale =
                    new Vector2(1f / Mathf.Max(0.01f, dashPeriod), 1f);

                lineRenderer.textureMode = LineTextureMode.Tile;
            }

            lineRenderer.material = mat;
        }

        public void SetAimColor(Color color)
        {
            baseAimColor = color;
            // nuqte thore roshan taake gehre background par saaf dikhein
            color = Color.Lerp(color, Color.white, 0.25f);
            startColor = color;
            endColor = color;
            dotColor = color;

            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }


        public void SetBounds(
            float leftX,
            float rightX,
            float ceilingY)
        {
            leftWallX = leftX;
            rightWallX = rightX;
            topCeilingY = ceilingY;
            boundsSet = true;
        }

        public void ProcessAimInput(
            Vector3 shooterPosition,
            bool isStateReady)
        {
            if (!isStateReady)
            {
                HideAimLine();
                isAiming = false;
                return;
            }

            Vector3 pointerWorldPos = Vector3.zero;
            bool hasInput = false;
            bool released = false;

            // Mobile touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pointerWorldPos = GetWorldPoint(touch.position);

                if (touch.phase == TouchPhase.Began ||
                    touch.phase == TouchPhase.Moved ||
                    touch.phase == TouchPhase.Stationary)
                {
                    hasInput = true;
                }
                else if (touch.phase == TouchPhase.Ended ||
                         touch.phase == TouchPhase.Canceled)
                {
                    released = true;
                }
            }
            // Mouse input
            else
            {
                pointerWorldPos = GetWorldPoint(Input.mousePosition);

                if (Input.GetMouseButton(0))
                    hasInput = true;

                if (Input.GetMouseButtonUp(0))
                    released = true;
            }

            // Pause / doosre UI button par tap se shot na chale
            if (hasInput && !isAiming && !pointerBlocked)
            {
                Vector2 screenPos = Input.touchCount > 0
                    ? Input.GetTouch(0).position
                    : (Vector2)Input.mousePosition;

                if (UIBuild.IsOverInteractableUI(screenPos))
                    pointerBlocked = true;
            }

            if (pointerBlocked)
            {
                if (released || (!hasInput && Input.touchCount == 0 && !Input.GetMouseButton(0)))
                    pointerBlocked = false;
                return;
            }

            if (hasInput)
            {
                Vector2 dir =
                    ((Vector2)pointerWorldPos -
                     (Vector2)shooterPosition).normalized;

                float angle =
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                if (angle < 0f)
                    angle += 360f;

                angle = Mathf.Clamp(
                    angle, minAimAngle, maxAimAngle);

                float rad = angle * Mathf.Deg2Rad;

                currentAimDirection = new Vector2(
                    Mathf.Cos(rad),
                    Mathf.Sin(rad)).normalized;

                isAiming = true;

                DrawTrajectory(
                    shooterPosition, currentAimDirection);
            }
            else if (released && isAiming)
            {
                isAiming = false;
                HideAimLine();
                OnAimReleased?.Invoke(currentAimDirection);
            }
        }

        private Vector3 GetWorldPoint(Vector3 screenPoint)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return Vector3.zero;

            screenPoint.z =
                -mainCamera.transform.position.z;

            return mainCamera.ScreenToWorldPoint(screenPoint);
        }

        private void DrawTrajectory(
            Vector3 startPos,
            Vector2 direction)
        {
            trajectoryPoints.Clear();

            float diameter =
                grid != null ? grid.BubbleDiameter : 0.8f;

            // Projectile jaisa hi hit distance (0.92) taake preview aur asli shot ek hon
            float hitRadius = diameter * 0.92f;
            float step = diameter * 0.1f;

            if (step < 0.01f ||
                float.IsNaN(direction.x) ||
                float.IsNaN(direction.y))
            {
                Debug.LogError(
                    "[AimController] Invalid trajectory input.");
                HideAimLine();
                return;
            }

            float left = LeftX;
            float right = RightX;
            float ceiling = CeilingY;

            Vector3 pos = startPos;
            Vector2 dir = direction;

            trajectoryPoints.Add(pos);

            float travelled = 0f;
            int bounces = 0;
            bool ended = false;
            bool endedOnCeiling = false;
            BubbleController hitTarget = null;

            int safetyCounter = 0;
            const int maxIterations = 2000;

            while (travelled < maxTraceLength &&
                   safetyCounter++ < maxIterations)
            {
                pos += (Vector3)(dir * step);
                travelled += step;

                // Bounce off the left wall
                if (pos.x < left)
                {
                    pos.x = left + (left - pos.x);
                    dir.x = -dir.x;

                    trajectoryPoints.Add(pos);

                    if (++bounces > maxBounces)
                        break;
                }
                // Bounce off the right wall
                else if (pos.x > right)
                {
                    pos.x = right - (pos.x - right);
                    dir.x = -dir.x;

                    trajectoryPoints.Add(pos);

                    if (++bounces > maxBounces)
                        break;
                }

                // Stop at ceiling or an existing bubble
                bool hitCeiling = pos.y >= ceiling;

                BubbleController hb = (!hitCeiling && grid != null)
                    ? grid.FindHitBubble(pos, hitRadius)
                    : null;

                if (hitCeiling || hb != null)
                {
                    trajectoryPoints.Add(pos);
                    ended = true;
                    endedOnCeiling = hitCeiling;
                    hitTarget = hb;
                    break;
                }
            }

            if (!ended)
                trajectoryPoints.Add(pos);

            TrajectoryEnd = pos;

            // Dots replace the solid line when enabled
            if (lineRenderer != null)
            {
                if (useDottedPreview && dotPrefab != null)
                {
                    lineRenderer.positionCount = 0;
                }
                else
                {
                    lineRenderer.positionCount =
                        trajectoryPoints.Count;

                    lineRenderer.SetPositions(
                        trajectoryPoints.ToArray());
                }
            }

            if (useDottedPreview && dotPrefab != null)
                UpdateDottedPreview();
            else
                HideDottedPreview();

            UpdateLandingMarker(pos);
            UpdateGhost(pos, ended, endedOnCeiling, hitTarget);
        }

        private void UpdateDottedPreview()
        {
            if (dotPrefab == null ||
                trajectoryPoints.Count < 2)
            {
                HideDottedPreview();
                return;
            }

            float diameterForDots = grid != null ? grid.BubbleDiameter : 0.4f;
            float spacing = Mathf.Max(0.02f, usingRuntimeDots ? diameterForDots * 0.55f : dotSpacing);
            float baseDotSize = usingRuntimeDots ? diameterForDots * 0.26f : dotSize;
            int dotIndex = 0;

            // Nuqte aage ki taraf "chalte" hue dikhte hain
            marchOffset = Mathf.Repeat(marchOffset + Time.deltaTime * spacing * 2.2f, spacing);

            float totalLength = 0f;
            for (int i = 1; i < trajectoryPoints.Count; i++)
                totalLength += Vector3.Distance(trajectoryPoints[i - 1], trajectoryPoints[i]);
            totalLength = Mathf.Max(0.001f, totalLength);
            float travelled = 0f;

            // Preserves dot spacing across bounce segments
            float distanceUntilNextDot = spacing - marchOffset + 0.001f;

            for (int i = 1;
                 i < trajectoryPoints.Count;
                 i++)
            {
                Vector3 from = trajectoryPoints[i - 1];
                Vector3 to = trajectoryPoints[i];

                float segmentLength =
                    Vector3.Distance(from, to);

                if (segmentLength <= 0.0001f)
                    continue;

                Vector3 segmentDirection =
                    (to - from).normalized;

                float distanceOnSegment = 0f;

                while (distanceOnSegment +
                       distanceUntilNextDot <= segmentLength)
                {
                    distanceOnSegment += distanceUntilNextDot;

                    if (dotIndex >= maxDots)
                        break;

                    Transform dot = GetOrCreateDot(dotIndex);

                    dot.position =
                        from + segmentDirection * distanceOnSegment;

                    float along = Mathf.Clamp01((travelled + distanceOnSegment) / totalLength);
                    float taper = Mathf.Lerp(1f, 0.55f, along);

                    // Shooter ke bilkul paas wale nuqte chhote (bubble ke andar na dikhein)
                    float nearFade = Mathf.Clamp01((travelled + distanceOnSegment) / (spacing * 1.5f));
                    dot.localScale = Vector3.one * (baseDotSize * taper * Mathf.Lerp(0.4f, 1f, nearFade));

                    SpriteRenderer sr =
                        dot.GetComponent<SpriteRenderer>();

                    if (sr != null)
                    {
                        Color c = dotColor;
                        c.a = dotColor.a * Mathf.Lerp(1f, 0.35f, along) * nearFade;
                        sr.color = c;
                        sr.sortingOrder = sortingOrder + 1;
                    }

                    dot.gameObject.SetActive(true);
                    dotIndex++;

                    distanceUntilNextDot = spacing;
                }

                if (dotIndex >= maxDots)
                    break;

                travelled += segmentLength;

                distanceUntilNextDot -=
                    segmentLength - distanceOnSegment;

                distanceUntilNextDot =
                    Mathf.Max(0.001f, distanceUntilNextDot);
            }

            for (int i = dotIndex; i < aimDots.Count; i++)
            {
                if (aimDots[i] != null)
                    aimDots[i].gameObject.SetActive(false);
            }
        }

        private Transform GetOrCreateDot(int index)
        {
            while (aimDots.Count <= index)
            {
                Transform dot = Instantiate(
                    dotPrefab, transform);

                dot.name = "AimDot_" + aimDots.Count;

                aimDots.Add(dot);
            }

            return aimDots[index];
        }

        private void UpdateLandingMarker(Vector3 endPos)
        {
            if (!showLandingMarker || dotPrefab == null)
            {
                HideLandingMarker();
                return;
            }

            if (landingMarker == null)
            {
                landingMarker = Instantiate(
                    dotPrefab, transform);

                landingMarker.name = "AimLandingMarker";
            }

            landingMarker.position = endPos;
            landingMarker.localScale =
                Vector3.one * landingMarkerSize;

            SpriteRenderer sr =
                landingMarker.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.color = landingMarkerColor;
                sr.sortingOrder = sortingOrder + 2;
            }

            landingMarker.gameObject.SetActive(true);
        }

        private void HideLandingMarker()
        {
            if (landingMarker != null)
                landingMarker.gameObject.SetActive(false);
        }

        private void UpdateGhost(Vector3 endPos, bool ended, bool onCeiling, BubbleController hitTarget)
        {
            if (ghostBubble == null || grid == null)
                return;

            if (!ended)
            {
                HideGhost();
                return;
            }

            // Wahi logic jo BubbleProjectile lagne par use karti hai
            GridCoordinate coord;

            if (onCeiling)
            {
                GridCoordinate raw = grid.WorldToGridPosition(endPos);
                coord = new GridCoordinate(0, raw.Column);
                if (!grid.IsEmpty(coord.Row, coord.Column))
                    coord = grid.FindNearestEmptyNeighbor(coord, endPos);
            }
            else if (hitTarget != null && hitTarget.CanBeDestroyedByDirectHit)
            {
                coord = hitTarget.GridPosition;   // khuli rassi wali bubble ki jagah le leti hai
            }
            else if (hitTarget != null)
            {
                coord = grid.FindAttachCellForHit(hitTarget, endPos);
            }
            else
            {
                coord = grid.WorldToGridPosition(endPos);
                coord = grid.FindNearestEmptyNeighbor(coord, endPos);
            }

            bool replaces = hitTarget != null && hitTarget.CanBeDestroyedByDirectHit && coord == hitTarget.GridPosition;

            if (!grid.IsValidCoordinate(coord) || (!grid.IsEmpty(coord.Row, coord.Column) && !replaces))
            {
                HideGhost();
                return;
            }

            if (ghostBubble.sprite != null)
            {
                float size =
                    ghostBubble.sprite.bounds.size.x;

                if (size > 0.0001f)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.04f;
                    ghostBubble.transform.localScale =
                        Vector3.one *
                        (grid.BubbleDiameter / size) * pulse;
                }
            }

            ghostBubble.transform.position =
                grid.GridToWorldPosition(coord);

            Color gc = baseAimColor;
            gc.a = 0.38f + Mathf.Sin(Time.time * 7f) * 0.08f;
            ghostBubble.color = gc;
            ghostBubble.enabled = true;

            if (runtimeGhostRing != null)
            {
                Color rc = Color.Lerp(baseAimColor, Color.white, 0.6f);
                rc.a = 0.85f;
                runtimeGhostRing.color = rc;
                runtimeGhostRing.enabled = true;
            }
        }

        private void HideGhost()
        {
            if (ghostBubble != null)
                ghostBubble.enabled = false;

            if (runtimeGhostRing != null)
                runtimeGhostRing.enabled = false;
        }

        private void HideDottedPreview()
        {
            foreach (Transform dot in aimDots)
            {
                if (dot != null)
                    dot.gameObject.SetActive(false);
            }
        }

        public void HideAimLine()
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;

            HideDottedPreview();
            HideLandingMarker();
            HideGhost();
        }

        private void OnDestroy()
        {
            // Clean up the runtime material created by this component
            if (lineRenderer != null &&
                lineRenderer.material != null)
            {
                Destroy(lineRenderer.material);
            }
        }
    }
}
