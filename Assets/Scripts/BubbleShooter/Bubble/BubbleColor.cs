using UnityEngine;

namespace BubbleShooter.Bubble
{
    public enum BubbleColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple
    }

    public static class BubbleColorUtility
    {
        public static Color GetColorValue(BubbleColor bubbleColor)
        {
            switch (bubbleColor)
            {
                case BubbleColor.Red:
                    return new Color(1f, 0.08f, 0.1f, 1f);
                case BubbleColor.Blue:
                    return new Color(0.1f, 0.2f, 1f, 1f);
                case BubbleColor.Green:
                    return new Color(0.1f, 0.85f, 0.9f, 1f);
                case BubbleColor.Yellow:
                    return new Color(1f, 0.92f, 0.05f, 1f);
                case BubbleColor.Purple:
                    return new Color(0.9f, 0.1f, 0.9f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
