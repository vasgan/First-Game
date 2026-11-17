using System;
using FirstGame;
using UnityEditor;
using UnityEngine;

namespace FirstGameEditor
{
    public static class AudioValidation
    {
        [MenuItem("First Game/Validate Audio")]
        public static void Validate()
        {
            float originalMusic = GameAudio.MusicVolume;
            float originalEffects = GameAudio.EffectsVolume;
            try
            {
                GameAudio.SetMusicVolume(0.37f);
                GameAudio.SetEffectsVolume(0.62f);
                Require(Mathf.Approximately(GameAudio.MusicVolume, 0.37f),
                    "Music volume must persist locally.");
                Require(Mathf.Approximately(GameAudio.EffectsVolume, 0.62f),
                    "Effects volume must persist locally.");

                Debug.Log("FIRST_GAME_AUDIO_VALIDATION_PASSED");
            }
            finally
            {
                GameAudio.SetMusicVolume(originalMusic);
                GameAudio.SetEffectsVolume(originalEffects);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Audio validation failed: " + message);
        }
    }
}
