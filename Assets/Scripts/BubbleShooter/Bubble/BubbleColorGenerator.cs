using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

namespace BubbleShooter.Bubble
{
    public class BubbleColorGenerator : MonoBehaviour
    {
        [SerializeField] private BubbleGrid bubbleGrid;

        public void Initialize(BubbleGrid grid)
        {
            bubbleGrid = grid;
        }

        public BubbleColor GetRandomColor(List<BubbleColor> allowedColors)
        {
            List<BubbleColor> activeGridColors = GetActiveGridColors(allowedColors);

            if (activeGridColors != null && activeGridColors.Count > 0)
            {
                int index = Random.Range(0, activeGridColors.Count);
                return activeGridColors[index];
            }

            if (allowedColors != null && allowedColors.Count > 0)
            {
                int index = Random.Range(0, allowedColors.Count);
                return allowedColors[index];
            }

            return (BubbleColor)Random.Range(0, 5);
        }

        private List<BubbleColor> GetActiveGridColors(List<BubbleColor> allowedColors)
        {
            if (bubbleGrid == null)
                return allowedColors;

            HashSet<BubbleColor> set = new HashSet<BubbleColor>();
            var allBubbles = bubbleGrid.GetAllBubbles();

            foreach (var b in allBubbles)
            {
                if (b != null && b.IsAttached)
                {
                    if (allowedColors == null || allowedColors.Contains(b.Color))
                    {
                        set.Add(b.Color);
                    }
                }
            }

            return new List<BubbleColor>(set);
        }
    }
}
