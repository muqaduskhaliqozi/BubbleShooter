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
        [Tooltip("Zaroori: bubble grid. Walls, ceiling aur bubble hit isi se aate hain.")]
        [SerializeField] private BubbleGrid grid;
        [Tooltip("Optional: semi-transparent bubble jo landing cell par dikhti hai")]
        [SerializeField] private SpriteRenderer ghostBubble;

        [Header("Aim Config")]
        [SerializeField] private float minAimAngle = 15f;  // Angle in degrees from positive X axis
        [SerializeField] private float maxAimAngle = 165f;

        [Header("Trajectory")]
        [SerializeField] private float maxTraceLength = 40f;
        [SerializeField] private int maxBounces = 10;

        [Header("Line Look")]
        [SerializeField] private float startWidth = 0.08f;
        [SerializeField] private float endWidth = 0.08f;
        [SerializeField] private Color startColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private bool dotted = false;
        [SerializeField] private float dashPeriod = 0.3f;
        [SerializeField] private int sortingOrder = 50;

        [Header("Fallback Bounds (sirf agar grid assign nahi)")]
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float topCeilingY = 3.8f;

        private Vector2 currentAimDirection = Vector2.up;
        private bool isAiming = false;

        // Last calculated trajectory (shooter isi path par bubble chala sakta hai)
        private readonly List<Vector3> trajectoryPoints = new List<Vector3>(16);

        public Vector2 CurrentAimDirection => currentAimDirection;
        public bool IsAiming => isAiming;

        /// <summary>Shooter se lekar landing point tak ke points (bounces ke saath).</summary>
        public IReadOnlyList<Vector3> TrajectoryPoints => trajectoryPoints;

        /// <summary>Jahan bubble ruk kar grid se chipkegi.</summary>
        public Vector3 TrajectoryEnd { get; private set; }

        public event Action<Vector2> OnAimReleased;

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
            if (lineRenderer == null) return;

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.sortingOrder = sortingOrder;

            Material mat = new Material(Shader.Find("Sprites/Default"));

            if (dotted)
            {
                // 1 pixel white, 1 pixel transparent = dashes
                Texture2D tex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.SetPixel(1, 0, new Color(1f, 1f, 1f, 0f));
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.Apply();

                mat.mainTexture = tex;
                mat.mainTextureScale = new Vector2(1f / dashPeriod, 1f);
                lineRenderer.textureMode = LineTextureMode.Tile;
            }

            lineRenderer.material = mat;
        }

        public void SetBounds(float leftX, float rightX, float ceilingY)
        {
            // Grid assign ho to grid ke bounds use hote hain
            leftWallX = leftX;
            rightWallX = rightX;
            topCeilingY = ceilingY;
        }

        private float LeftX => grid != null ? grid.LeftWallX : leftWallX;
        private float RightX => grid != null ? grid.RightWallX : rightWallX;
        private float CeilingY => grid != null ? grid.CeilingY : topCeilingY;

        public void ProcessAimInput(Vector3 shooterPosition, bool isStateReady)
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

            // Mobile Touch Input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pointerWorldPos = GetWorldPoint(touch.position);

                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    hasInput = true;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    released = true;
                }
            }
            // Mouse Input
            else
            {
                pointerWorldPos = GetWorldPoint(Input.mousePosition);

                if (Input.GetMouseButton(0))
                {
                    hasInput = true;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    released = true;
                }
            }

            if (hasInput)
            {
                Vector2 dir = ((Vector2)pointerWorldPos - (Vector2)shooterPosition).normalized;

                // Clamp shooting angle (prevent shooting downwards)
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                angle = Mathf.Clamp(angle, minAimAngle, maxAimAngle);
                float rad = angle * Mathf.Deg2Rad;
                currentAimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

                isAiming = true;
                DrawTrajectory(shooterPosition, currentAimDirection);
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

            screenPoint.z = -mainCamera.transform.position.z;
            return mainCamera.ScreenToWorldPoint(screenPoint);
        }

        /// <summary>
        /// Chhote steps mein bubble ka rasta simulate karta hai:
        /// walls par bounce, aur ceiling ya kisi grid bubble se takrane par ruk jata hai.
        /// Physics raycast use nahi hota, isliye collider/scale ka koi masla nahi.
        /// </summary>
        private void DrawTrajectory(Vector3 startPos, Vector2 direction)
        {
            trajectoryPoints.Clear();

            float diameter = grid != null ? grid.BubbleDiameter : 0.8f;
            float hitRadius = diameter * 0.85f;
            float step = diameter * 0.2f;

            // Safety: step 0 ho ya direction NaN ho to loop kabhi khatam nahi hota (Unity freeze)
            if (step < 0.01f || float.IsNaN(direction.x) || float.IsNaN(direction.y))
            {
                Debug.LogError("[AimController] Invalid trajectory input (BubbleDiameter 0 ya direction NaN). Grid check karo.");
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

            while (travelled < maxTraceLength && safetyCounter++ < maxIterations)
            {
                pos += (Vector3)(dir * step);
                travelled += step;

                // Wall bounce
                if (pos.x < left)
                {
                    pos.x = left + (left - pos.x);
                    dir.x = -dir.x;
                    trajectoryPoints.Add(pos);
                    if (++bounces > maxBounces) break;
                }
                else if (pos.x > right)
                {
                    pos.x = right - (pos.x - right);
                    dir.x = -dir.x;
                    trajectoryPoints.Add(pos);
                    if (++bounces > maxBounces) break;
                }

                // Ceiling ya bubble hit
                bool hitCeiling = pos.y >= ceiling;
                bool hitBubble = grid != null && grid.FindHitBubble(pos, hitRadius) != null;

                if (hitCeiling || hitBubble)
                {
                    trajectoryPoints.Add(pos);
                    ended = true;
                    break;
                }
            }

            if (!ended) trajectoryPoints.Add(pos);

            TrajectoryEnd = pos;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = trajectoryPoints.Count;
                lineRenderer.SetPositions(trajectoryPoints.ToArray());
            }

            UpdateGhost(pos);
        }

        private void UpdateGhost(Vector3 endPos)
        {
            if (ghostBubble == null || grid == null) return;

            GridCoordinate coord = grid.WorldToGridPosition(endPos);
            coord = grid.FindNearestEmptyNeighbor(coord, endPos);

            if (!grid.IsEmpty(coord.Row, coord.Column))
            {
                ghostBubble.enabled = false;
                return;
            }

            if (ghostBubble.sprite != null)
            {
                float size = ghostBubble.sprite.bounds.size.x;
                if (size > 0.0001f)
                    ghostBubble.transform.localScale = Vector3.one * (grid.BubbleDiameter / size);
            }

            ghostBubble.transform.position = grid.GridToWorldPosition(coord);
            ghostBubble.color = new Color(1f, 1f, 1f, 0.35f);
            ghostBubble.enabled = true;
        }

        public void HideAimLine()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }

            if (ghostBubble != null)
            {
                ghostBubble.enabled = false;
            }
        }
    }
}