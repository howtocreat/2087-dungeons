using System.Collections.Generic;
using UnityEngine;

namespace Dungeons2087
{
    internal sealed class DungeonSound
    {
        private const int SampleRate = 22050;
        private readonly AudioSource source;
        private readonly Dictionary<string, AudioClip> cues = new();
        private readonly AudioClip ambience;

        public DungeonSound(AudioSource audioSource)
        {
            source = audioSource;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.18f;
            source.loop = true;
            ambience = Generate("Shelter ventilation", 4f, (time, noise) =>
                0.07f * (Wave(42f, time) + Wave(57f, time) * 0.35f) * (0.8f + 0.2f * Wave(0.5f, time))
                + noise * 0.006f);
            source.clip = ambience;
            source.Play();

            cues.Add("mine-silver", Chime("Silver", 310f, 410f, 0.17f, 0.24f));
            cues.Add("mine-gold", Chime("Gold", 430f, 645f, 0.29f, 0.17f));
            cues.Add("mine-diamond", Chime("Diamond", 560f, 840f, 0.42f, 0.09f));
            cues.Add("hit", Chime("Impact", 53f, 39f, 0.27f, 0.35f));
            cues.Add("dodge", Chime("Dodge", 195f, 148f, 0.16f, 0.3f));
            cues.Add("victory", Chime("Victory", 392f, 587f, 0.65f, 0f));
            cues.Add("food", Chime("Recover", 440f, 660f, 0.34f, 0.02f));
            cues.Add("death", Chime("Shutdown", 116f, 58f, 0.85f, 0.12f));
        }

        public void Play(string cueName)
        {
            if (!string.IsNullOrEmpty(cueName) && cues.TryGetValue(cueName, out AudioClip cue))
            {
                source.PlayOneShot(cue, 0.65f);
            }
        }

        public void Dispose()
        {
            Object.Destroy(ambience);
            foreach (AudioClip clip in cues.Values)
            {
                Object.Destroy(clip);
            }
        }

        private static AudioClip Chime(string name, float low, float high, float duration, float grit)
        {
            return Generate(name, duration, (time, noise) =>
            {
                float progress = time / duration;
                float decay = Mathf.Min(1f, time * 350f) * Mathf.Pow(1f - progress, 2.4f);
                float pitch = Wave(low, time) * 0.42f + Wave(high, time) * 0.28f * Mathf.Min(progress * 7f, 1f);
                return (pitch + noise * grit * (1f - progress)) * decay;
            });
        }

        private static float Wave(float frequency, float time) => Mathf.Sin(time * frequency * Mathf.PI * 2f);

        private static AudioClip Generate(string name, float duration, System.Func<float, float, float> sample)
        {
            int count = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[count];
            var random = new System.Random(2087);
            for (int i = 0; i < count; i++)
            {
                data[i] = sample(i / (float)SampleRate, (float)random.NextDouble() * 2f - 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
