using System;
using UnityEngine;

namespace BubbleShooter.Score
{
    public class ScoreManager : MonoBehaviour
    {
        private int currentScore = 0;
        private int comboCount = 0;

        public int CurrentScore => currentScore;
        public int ComboCount => comboCount;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnComboChanged;

        public void ResetScore()
        {
            currentScore = 0;
            comboCount = 0;
            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(comboCount);
        }

        public void AddMatchScore(int poppedBubbleCount)
        {
            comboCount++;
            int baseScore = poppedBubbleCount * 10;
            int bonus = (comboCount - 1) * 15;
            int total = baseScore + bonus;

            currentScore += total;
            Debug.Log($"[BubbleShooter] Match score added: +{total} (Base: {baseScore}, Combo: {comboCount})");

            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(comboCount);
        }

        public void AddFloatingScore(int floatingBubbleCount)
        {
            int total = floatingBubbleCount * 20;
            currentScore += total;
            Debug.Log($"[BubbleShooter] Floating score added: +{total}");

            OnScoreChanged?.Invoke(currentScore);
        }

        public void ResetCombo()
        {
            comboCount = 0;
            OnComboChanged?.Invoke(comboCount);
        }
    }
}
