using System;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

namespace BubbleShooter.Shooter
{
    public class BubbleProjectile : MonoBehaviour
    {
        [Header("Projectile Config")]
        [SerializeField] private float speed = 14f;
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float ceilingY = 3.8f;

        [Header("Collision")]
        [Tooltip("Hit distance = bubble diameter x yeh value. 0.92 aam tor par sahi rehta hai.")]
        [SerializeField] private float hitDistanceFactor = 0.92f;

        private Vector2 direction;
        private bool isMoving = false;
        private BubbleController bubbleController;
        private BubbleGrid bubbleGrid;
        private Action<BubbleController, GridCoordinate> onAttachedCallback;

        public bool IsMoving => isMoving;

        public void Initialize(BubbleController controller, BubbleGrid grid, float leftX, float rightX, float topY, Action<BubbleController, GridCoordinate> onAttached)
        {
            bubbleController = controller;
            bubbleGrid = grid;
            leftWallX = leftX;
            rightWallX = rightX;
            ceilingY = topY;
            onAttachedCallback = onAttached;
        }

        public void Launch(Vector2 dir, float moveSpeed)
        {
            direction = dir.normalized;
            speed = moveSpeed;
            isMoving = true;
            if (bubbleController != null)
            {
                bubbleController.SetMoving(true);
            }
        }

        private void Update()
        {
            if (!isMoving || bubbleController == null || bubbleGrid == null)
                return;

            Vector3 nextPos = transform.position + (Vector3)(direction * speed * Time.deltaTime);

            // 1. Wall Bouncing
            if (nextPos.x <= leftWallX)
            {
                nextPos.x = leftWallX + (leftWallX - nextPos.x);
                direction.x = Mathf.Abs(direction.x);
            }
            else if (nextPos.x >= rightWallX)
            {
                nextPos.x = rightWallX - (nextPos.x - rightWallX);
                direction.x = -Mathf.Abs(direction.x);
            }

            transform.position = nextPos;

            // 2. Ceiling Detection
            if (nextPos.y >= ceilingY)
            {
                SnapToGrid(nextPos, true, null);
                return;
            }

            // 3. Collision: sab se qareeb wali bubble dhoondo (pehli mili wali nahi)
            float hitDistance = bubbleGrid.BubbleDiameter * hitDistanceFactor;
            BubbleController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (BubbleController b in bubbleGrid.GetAllBubbles())
            {
                if (b == null || !b.IsAttached || b == bubbleController) continue;

                float dist = Vector2.Distance(nextPos, b.transform.position);
                if (dist <= hitDistance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = b;
                }
            }

            if (nearest != null)
            {
                SnapToGrid(nextPos, false, nearest);
            }
        }

        private void SnapToGrid(Vector3 hitPosition, bool isCeilingHit, BubbleController hitBubble = null)
        {
            isMoving = false;
            if (bubbleController != null)
            {
                bubbleController.SetMoving(false);
            }

            GridCoordinate targetCoord;

            if (isCeilingHit)
            {
                // Snap directly to row 0 top position
                GridCoordinate rawCoord = bubbleGrid.WorldToGridPosition(hitPosition);
                targetCoord = new GridCoordinate(0, rawCoord.Column);
                if (!bubbleGrid.IsEmpty(targetCoord.Row, targetCoord.Column))
                {
                    targetCoord = bubbleGrid.FindNearestEmptyNeighbor(targetCoord, hitPosition);
                }
            }
            else
            {
                if (hitBubble != null)
                {
                    // Hit wali bubble ke seedhe khali padosi mein hi lagao
                    targetCoord = bubbleGrid.FindAttachCellForHit(hitBubble, hitPosition);
                }
                else
                {
                    GridCoordinate rawCoord = bubbleGrid.WorldToGridPosition(hitPosition);
                    targetCoord = bubbleGrid.FindNearestEmptyNeighbor(rawCoord, hitPosition);
                }
            }

            // Fallback if target cell is invalid or occupied
            if (!bubbleGrid.IsValidCoordinate(targetCoord) || !bubbleGrid.IsEmpty(targetCoord.Row, targetCoord.Column))
            {
                targetCoord = bubbleGrid.WorldToGridPosition(hitPosition);
            }

            bool added = bubbleGrid.AddBubble(targetCoord.Row, targetCoord.Column, bubbleController);
            if (!added)
            {
                Debug.LogWarning($"[BubbleProjectile] Failed to attach bubble to grid at {targetCoord}.");
            }

            Destroy(this); // Remove projectile script component after snapping
            onAttachedCallback?.Invoke(bubbleController, targetCoord);
        }
    }
}