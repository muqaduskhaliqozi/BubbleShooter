
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;

namespace BubbleShooter.Level
{
    [CreateAssetMenu(
        fileName = "BubbleLevelData",
        menuName = "BubbleShooter/Level Data")]
    public class BubbleLevelData : ScriptableObject
    {
        [Header("Level")]
        public int levelNumber = 1;

        [Header("Fixed Grid Settings")]
        [Tooltip("Starting rows filled with bubbles.")]
        public int startingRows = 4;

        [Tooltip("Maximum active bubbles per row.")]
        public int activeColumns = 4;

        [Tooltip("Keep the grid width fixed across levels.")]
        public int gridColumns = 8;

        [Header("Pending Rows")]
        public int pendingRows = 6;
        public int shotsPerNewRow = 3;

        [Header("Bubble Colors")]
        public List<BubbleColor> availableColors =
            new List<BubbleColor>
            {
                BubbleColor.Red,
                BubbleColor.Blue,
                BubbleColor.Green
            };

        [Header("Matching")]
        public int minimumMatchCount = 3;
        public int maximumMissedShots = 5;

        [Header("Legacy Settings")]
        public int rows = 4;
        public int columns = 4;
        public bool enableMissRows = false;
        public float bubbleSpeed = 14f;
        public int dangerRow = 10;

        [Header("Optional Initial Formation")]
        public List<BubbleColor> initialGridColors =
            new List<BubbleColor>();
    }
}