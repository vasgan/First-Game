using UnityEngine;

namespace FirstGame
{
    /// <summary>
    /// Local, procedural audio feedback. Keeping the clips in code lets the prototype
    /// provide distinct feedback without introducing platform-specific asset loading.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MusicVolumeKey = "FirstGame.MusicVolume";
        private const string EffectsVolumeKey = "FirstGame.EffectsVolume";
        private const int SampleRate = 22050;

        private static GameAudio instance;
        private AudioSource musicSource;
        private AudioSource effectsSource;
        private AudioClip buildClip;
        private AudioClip upgradeClip;
        private AudioClip attackClip;
        private AudioClip victoryClip;
        private AudioClip defeatClip;

        public static float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.65f);
        public static float EffectsVolume => PlayerPrefs.GetFloat(EffectsVolumeKey, 0.8f);

        public static GameAudio EnsureInstance()
        {
            if (instance) return instance;

            GameObject root = new GameObject("Game Audio");
            instance = root.AddComponent<GameAudio>();
            DontDestroyOnLoad(root);
            return instance;
        }

        public static void SetMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            if (instance) instance.ApplyVolumes();
        }

        public static void SetEffectsVolume(float value)
        {
            PlayerPrefs.SetFloat(EffectsVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            if (instance) instance.ApplyVolumes();
        }

        public static void PlayBuild() => EnsureInstance().Play(EnsureInstance().buildClip);
        public static void PlayUpgrade() => EnsureInstance().Play(EnsureInstance().upgradeClip);
        public static void PlayAttack() => EnsureInstance().Play(EnsureInstance().attackClip, 0.45f);
        public static void PlayResult(bool won) => EnsureInstance().Play(won
            ? EnsureInstance().victoryClip : EnsureInstance().defeatClip);

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;

            buildClip = CreateTone("Build", new[] { 440f, 660f }, 0.22f);
            upgradeClip = CreateTone("Upgrade", new[] { 523f, 659f, 784f }, 0.3f);
            attackClip = CreateTone("Attack", new[] { 350f }, 0.08f);
            victoryClip = CreateTone("Victory", new[] { 523f, 659f, 784f, 1047f }, 0.52f);
            defeatClip = CreateTone("Defeat", new[] { 392f, 330f, 262f }, 0.48f);
            musicSource.clip = CreateMusicLoop();
            ApplyVolumes();
            musicSource.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void ApplyVolumes()
        {
            if (musicSource) musicSource.volume = MusicVolume;
            if (effectsSource) effectsSource.volume = EffectsVolume;
        }

        private void Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip && effectsSource) effectsSource.PlayOneShot(clip, volumeScale);
        }

        private static AudioClip CreateTone(string clipName, float[] frequencies, float seconds)
        {
            int samples = Mathf.CeilToInt(seconds * SampleRate);
            float[] data = new float[samples];
            int noteLength = samples / frequencies.Length;
            for (int sample = 0; sample < samples; sample++)
            {
                int note = Mathf.Min(frequencies.Length - 1, sample / Mathf.Max(1, noteLength));
                float time = sample / (float)SampleRate;
                float local = (sample % Mathf.Max(1, noteLength)) / (float)Mathf.Max(1, noteLength);
                float envelope = Mathf.Clamp01(local * 12f) * Mathf.Clamp01((1f - local) * 9f);
                data[sample] = Mathf.Sin(time * frequencies[note] * Mathf.PI * 2f) * envelope * 0.28f;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMusicLoop()
        {
            float[] notes = { 220f, 261.63f, 329.63f, 293.66f, 220f, 246.94f, 293.66f, 196f };
            const float secondsPerNote = 0.5f;
            int noteLength = Mathf.RoundToInt(SampleRate * secondsPerNote);
            float[] data = new float[noteLength * notes.Length];
            for (int sample = 0; sample < data.Length; sample++)
            {
                int note = sample / noteLength;
                float local = (sample % noteLength) / (float)noteLength;
                float envelope = Mathf.Clamp01(local * 10f) * Mathf.Clamp01((1f - local) * 4f);
                float time = sample / (float)SampleRate;
                float fundamental = Mathf.Sin(time * notes[note] * Mathf.PI * 2f);
                float overtone = Mathf.Sin(time * notes[note] * Mathf.PI * 4f) * 0.25f;
                data[sample] = (fundamental + overtone) * envelope * 0.10f;
            }

            AudioClip clip = AudioClip.Create("BackgroundLoop", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
