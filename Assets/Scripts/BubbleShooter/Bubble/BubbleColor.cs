using UnityEngine;

namespace BubbleShooter.Bubble
{
    public enum BubbleColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        BrightGreen,

        // ---- Naye colors: HAMESHA list ke aakhir mein add karo. ----
        // Unity enum ko number (0,1,2...) ki shakal mein save karta hai, is liye
        // beech mein naya color daalne se purane levels ke colors badal jate hain.
        Orange,   // Level 9
        White     // Level 9
    }

    public static class BubbleColorUtility
    {
        public static Color GetColorValue(BubbleColor bubbleColor)
        {
            switch (bubbleColor)
            {
                case BubbleColor.Red:
                    return new Color(1f, 0.16f, 0.2f, 1f);

                case BubbleColor.Blue:
                    return new Color(0.2f, 0.45f, 1f, 1f);

                // Purane levels ka existing color preserve kiya hai (ye asal mein cyan hai).
                case BubbleColor.Green:
                    return new Color(0.15f, 0.9f, 0.95f, 1f);

                case BubbleColor.Yellow:
                    return new Color(1f, 0.88f, 0.12f, 1f);

                case BubbleColor.Purple:
                    return new Color(0.75f, 0.3f, 1f, 1f);

                // Level 7 ke liye alag bright green.
                case BubbleColor.BrightGreen:
                    return new Color(0.25f, 0.92f, 0.25f, 1f);

                // Level 9: orange (yellow se alag, zyada gehra)
                case BubbleColor.Orange:
                    return new Color(1f, 0.55f, 0.05f, 1f);

                // Level 9: safed (halka sa grey taake dark background par saaf dikhe)
                case BubbleColor.White:
                    return new Color(0.95f, 0.95f, 0.97f, 1f);

                default:
                    return Color.white;
            }
        }
    }
}