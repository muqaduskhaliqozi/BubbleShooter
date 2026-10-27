
using System;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Core;
using BubbleShooter.Grid;

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

        public Vector2 CurrentAimDirection => currentAimDirection;
        public bool IsAiming => isAiming;

        public IReadOnlyList<Vector3> TrajectoryPoints =>
            trajectoryPoints;

        public Vector3 TrajectoryEnd { get; private set; }

        public event Action<Vector2> OnAimReleased;

        private float LeftX =>
            grid != null ? grid.LeftWallX : leftWallX;

        private float RightX =>
            grid != null ? grid.RightWallX : rightWallX;

        private float CeilingY =>
            grid != null ? grid.CeilingY : topCeilingY;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (grid == null)
                grid = FindObjectOfType<BubbleGrid>();

            SetupLineRenderer();
            HideAimLine();
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

            float hitRadius = diameter * 0.85f;
            float step = diameter * 0.2f;

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

                bool hitBubble =
                    grid != null &&
                    grid.FindHitBubble(pos, hitRadius) != null;

                if (hitCeiling || hitBubble)
                {
                    trajectoryPoints.Add(pos);
                    ended = true;
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
            UpdateGhost(pos);
        }

        private void UpdateDottedPreview()
        {
            if (dotPrefab == null ||
                trajectoryPoints.Count < 2)
            {
                HideDottedPreview();
                return;
            }

            float spacing = Mathf.Max(0.02f, dotSpacing);
            int dotIndex = 0;

            // Preserves dot spacing across bounce segments
            float distanceUntilNextDot = spacing;

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

                    dot.localScale = Vector3.one * dotSize;

                    SpriteRenderer sr =
                        dot.GetComponent<SpriteRenderer>();

                    if (sr != null)
                    {
                        sr.color = dotColor;
                        sr.sortingOrder = sortingOrder + 1;
                    }

                    dot.gameObject.SetActive(true);
                    dotIndex++;

                    distanceUntilNextDot = spacing;
                }

                if (dotIndex >= maxDots)
                    break;

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

        private void UpdateGhost(Vector3 endPos)
        {
            if (ghostBubble == null || grid == null)
                return;

            GridCoordinate coord =
                grid.WorldToGridPosition(endPos);

            coord = grid.FindNearestEmptyNeighbor(
                coord, endPos);

            if (!grid.IsEmpty(coord.Row, coord.Column))
            {
                ghostBubble.enabled = false;
                return;
            }

            if (ghostBubble.sprite != null)
            {
                float size =
                    ghostBubble.sprite.bounds.size.x;

                if (size > 0.0001f)
                {
                    ghostBubble.transform.localScale =
                        Vector3.one *
                        (grid.BubbleDiameter / size);
                }
            }

            ghostBubble.transform.position =
                grid.GridToWorldPosition(coord);

            ghostBubble.color =
                new Color(1f, 1f, 1f, 0.35f);

            ghostBubble.enabled = true;
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

            if (ghostBubble != null)
                ghostBubble.enabled = false;
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
