using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleShooter.Level
{
    /// <summary>
    /// Level ke shape ka design, text se draw karo. UPAR wali row pehle likho.
    ///
    ///   X, #, O      = bubble (random color, level ke colors mein se)
    ///   1 se 9       = fixed color (1 = level ke colors ki pehli, 2 = dusri, ...)
    ///   .  _  space  = khali jagah
    ///   ---          = ALAG LINE: is line ke UPAR ki rows shuru mein nahi dikhengi,
    ///                  wo baad mein upar se ek ek karke aayengi (shots ke hisab se).
    ///                  Is line ke NEECHE ki rows shuru mein screen par hongi.
    ///
    /// Misal: 2 triangles, pehle neeche wala dikhe, upar wala baad mein aaye:
    ///   XXXXXXXXX
    ///   .XXXXXXX.
    ///   ..XXXXX..
    ///   ...XXX...
    ///   ....X....
    ///   ---
    ///   XXXXXXXXX
    ///   .XXXXXXX.
    ///   ..XXXXX..
    ///   ...XXX...
    ///   ....X....
    ///
    /// Baad mein aane wali rows is tarteeb se aati hain: ---- ke sab se qareeb wali row pehle,
    /// phir uske upar wali, ... aakhir mein sab se upar wali. Is tarah dono shapes ek ke upar ek
    /// bilkul waise ban jate hain jaisa aapne likha.
    ///
    /// ZAROORI: har bubble row 0 se (seedhi jurkar) judi honi chahiye, warna wo floating hai.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelShape", menuName = "BubbleShooter/Level Shape")]
    public class LevelShapeData : ScriptableObject
    {
        [TextArea(8, 24)]
        [SerializeField]
        private string pattern =
            "XXXXXXXXX\n" +
            ".XXXXXXX.\n" +
            "..XXXXX..\n" +
            "...XXX...\n" +
            "....X....";

        private static bool IsSeparator(string line)
        {
            string t = line.Trim();
            if (t.Length < 3) return false;

            foreach (char c in t)
            {
                if (c != '-' && c != '=') return false;
            }
            return true;
        }

        /// <summary>Pattern ki saari lines (upar se neeche), separator ke baghair.</summary>
        public string[] GetLines()
        {
            List<string> all = ReadAllLines();
            List<string> lines = new List<string>();

            foreach (string line in all)
            {
                if (!IsSeparator(line)) lines.Add(line);
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Pattern ko do hisson mein baantta hai:
        ///  - initialLines: shuru mein screen par (separator ke neeche wali rows, ya agar separator nahi to sab)
        ///  - pendingLines: baad mein upar se aane wali rows, aane ki tarteeb mein
        ///    (separator ke qareeb wali pehle)
        /// </summary>
        public void GetParts(out string[] initialLines, out string[] pendingLines)
        {
            List<string> all = ReadAllLines();

            int separatorIndex = -1;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsSeparator(all[i]))
                {
                    separatorIndex = i;
                    break;
                }
            }

            if (separatorIndex < 0)
            {
                initialLines = GetLines();
                pendingLines = new string[0];
                return;
            }

            List<string> top = new List<string>();      // separator ke upar
            List<string> bottom = new List<string>();   // separator ke neeche

            for (int i = 0; i < all.Count; i++)
            {
                if (IsSeparator(all[i])) continue;

                if (i < separatorIndex) top.Add(all[i]);
                else bottom.Add(all[i]);
            }

            top.Reverse();   // separator ke qareeb wali row pehle aaye

            initialLines = bottom.ToArray();
            pendingLines = top.ToArray();
        }

        /// <summary>Sab se lambi line ki length (shape ki chaurai), saari lines ko mila kar.</summary>
        public int MaxLineLength
        {
            get
            {
                int max = 0;
                foreach (string line in GetLines())
                    max = Mathf.Max(max, line.Length);
                return max;
            }
        }

        private List<string> ReadAllLines()
        {
            List<string> lines = new List<string>();

            if (string.IsNullOrEmpty(pattern))
                return lines;

            lines.AddRange(pattern.Replace("\r", "").Split('\n'));

            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);

            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }
    }

    /// <summary>Kaunsa level (BubbleLevelData) kaunsa shape use kare. Shape na ho to normal rows.</summary>
    [Serializable]
    public class LevelShapeEntry
    {
        public BubbleLevelData level;
        public LevelShapeData shape;
    }
}