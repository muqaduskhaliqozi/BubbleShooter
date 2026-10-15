using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

namespace BubbleShooter.Matching
{
    public class BubbleMatchFinder : MonoBehaviour
    {
        [SerializeField] private BubbleGrid bubbleGrid;

        public void Initialize(BubbleGrid grid)
        {
            bubbleGrid = grid;
        }

        public List<BubbleController> FindMatches(BubbleController startBubble, int minimumMatchCount = 3)
        {
            List<BubbleController> matches = new List<BubbleController>();
            if (startBubble == null || bubbleGrid == null)
                return matches;

            BubbleColor targetColor = startBubble.Color;
            Queue<GridCoordinate> queue = new Queue<GridCoordinate>();
            HashSet<GridCoordinate> visited = new HashSet<GridCoordinate>();

            GridCoordinate startCoord = startBubble.GridPosition;
            queue.Enqueue(startCoord);
            visited.Add(startCoord);

            while (queue.Count > 0)
            {
                GridCoordinate currentCoord = queue.Dequeue();
                BubbleController currentBubble = bubbleGrid.GetBubble(currentCoord);

                if (currentBubble != null && currentBubble.Color == targetColor && currentBubble.IsAttached)
                {
                    matches.Add(currentBubble);

                    List<GridCoordinate> neighbors = bubbleGrid.GetNeighbors(currentCoord);
                    foreach (var n in neighbors)
                    {
                        if (!visited.Contains(n))
                        {
                            visited.Add(n);
                            BubbleController neighborBubble = bubbleGrid.GetBubble(n);
                            if (neighborBubble != null && neighborBubble.Color == targetColor && neighborBubble.IsAttached)
                            {
                                queue.Enqueue(n);
                            }
                        }
                    }
                }
            }

            if (matches.Count >= minimumMatchCount)
            {
                return matches;
            }

            return new List<BubbleController>();
        }
    }
}
