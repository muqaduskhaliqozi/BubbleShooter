using UnityEngine;

namespace BubbleShooter.Core
{
    /// <summary>
    /// Settings (Sound / Music) jo Main Menu ke gear button se badalti hain. PlayerPrefs mein save.
    /// Sound band = poori game ki awaz band (AudioListener.volume = 0).
    /// Music ki setting un scripts ke liye hai jo aage music bajayengi (GameSettings.MusicOn dekhein).
    /// </summary>
    public static class GameSettings
    {
        private const string SoundKey = "BubbleShooter_Sound";
        private const string MusicKey = "BubbleShooter_Music";

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Apply();
            }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Apply()
        {
            AudioListener.volume = SoundOn ? 1f : 0f;
        }
    }
}
