using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

namespace BubbleShooter.Matching
{
    public class FloatingBubbleDetector : MonoBehaviour
    {
        [SerializeField] private BubbleGrid bubbleGrid;

        public void Initialize(BubbleGrid grid)
        {
            bubbleGrid = grid;
        }

        public List<BubbleController> FindFloatingBubbles()
        {
            List<BubbleController> floatingBubbles = new List<BubbleController>();
            if (bubbleGrid == null)
                return floatingBubbles;

            List<BubbleController> allBubbles = bubbleGrid.GetAllBubbles();
            if (allBubbles.Count == 0)
                return floatingBubbles;

            HashSet<GridCoordinate> connectedToCeiling = new HashSet<GridCoordinate>();
            Queue<GridCoordinate> queue = new Queue<GridCoordinate>();

            // Seed queue with all attached bubbles in top row (Row 0)
            for (int c = 0; c < bubbleGrid.MaxColumns; c++)
            {
                BubbleController topBubble = bubbleGrid.GetBubble(0, c);
                if (topBubble != null && topBubble.IsAttached)
                {
                    GridCoordinate coord = topBubble.GridPosition;
                    connectedToCeiling.Add(coord);
                    queue.Enqueue(coord);
                }
            }

            // BFS from top row
            while (queue.Count > 0)
            {
                GridCoordinate currentCoord = queue.Dequeue();
                List<GridCoordinate> neighbors = bubbleGrid.GetNeighbors(currentCoord);

                foreach (var n in neighbors)
                {
                    if (!connectedToCeiling.Contains(n))
                    {
                        BubbleController neighborBubble = bubbleGrid.GetBubble(n);
                        if (neighborBubble != null && neighborBubble.IsAttached)
                        {
                            connectedToCeiling.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                }
            }

            // Any bubble not connected to ceiling is floating!
            foreach (var bubble in allBubbles)
            {
                if (bubble != null && bubble.IsAttached)
                {
                    if (!connectedToCeiling.Contains(bubble.GridPosition))
                    {
                        floatingBubbles.Add(bubble);
                    }
                }
            }

            return floatingBubbles;
        }
    }
}
