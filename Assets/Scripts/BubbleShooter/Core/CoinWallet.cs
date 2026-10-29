using System;
using UnityEngine;

namespace BubbleShooter.Core
{
    /// <summary>
    /// Player ke coins (PlayerPrefs mein save). Level jeetne par coins milte hain,
    /// Main Menu ke upar daayein coin counter mein dikhte hain.
    /// </summary>
    public static class CoinWallet
    {
        private const string Key = "BubbleShooter_Coins";

        /// <summary>Har jeete hue level par kitne coins.</summary>
        public const int CoinsPerLevelWin = 10;

        public static event Action<int> OnChanged;

        public static int Coins => PlayerPrefs.GetInt(Key, 0);

        public static void Add(int amount)
        {
            if (amount == 0) return;
            int value = Mathf.Max(0, Coins + amount);
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
            OnChanged?.Invoke(value);
        }

        /// <summary>Kharch karo; kaafi coins na hon to false.</summary>
        public static bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Add(-amount);
            return true;
        }
    }
}
