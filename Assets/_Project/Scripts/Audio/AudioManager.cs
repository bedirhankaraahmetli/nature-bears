using System.Collections;
using NatureBears.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace NatureBears.Audio
{
    public enum AudioChannel
    {
        Master,
        BGM,
        Ambient,
        SFX
    }

    /// <summary>
    /// AudioMixer-driven audio hub.
    /// Channels: BGM (lo-fi acoustic guitar / soft marimba), Ambient (ASMR
    /// organic loops — crackling fire, stream), SFX (tactile — twig snaps,
    /// bubble pops for UI).
    ///
    /// SETUP (in-editor, later): create Assets/_Project/Audio/MainMixer.mixer
    /// with groups BGM/Ambient/SFX under Master and expose the params
    /// "MasterVolume", "BGMVolume", "AmbientVolume", "SFXVolume".
    /// Everything below is null-guarded so the project runs with placeholders
    /// (or no mixer at all) until then.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer (author in-editor; exposed params: MasterVolume, BGMVolume, AmbientVolume, SFXVolume)")]
        [SerializeField] private AudioMixer mixer;

        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource ambientSource;
        [Tooltip("Small round-robin pool so overlapping taps don't cut each other off.")]
        [SerializeField] private AudioSource[] sfxSources;

        private int _nextSfxIndex;
        private Coroutine _bgmFadeRoutine;

        private static readonly string[] ExposedParams =
        {
            "MasterVolume", "BGMVolume", "AmbientVolume", "SFXVolume"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ------------------------------------------------------------------
        // Playback
        // ------------------------------------------------------------------

        /// <summary>Cross-fades to a new BGM track. Placeholder clips are fine — swap assets later.</summary>
        public void PlayBGM(AudioClip clip, float fadeSeconds = 1f)
        {
            if (bgmSource == null || clip == null || bgmSource.clip == clip) return;

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = StartCoroutine(FadeToBGM(clip, Mathf.Max(0.01f, fadeSeconds)));
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null || ambientSource.clip == clip) return;

            ambientSource.clip = clip;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        /// <summary>Fire-and-forget SFX. Optional pitch variance keeps repeated taps organic.</summary>
        public void PlaySFX(AudioClip clip, float pitchVariance = 0f)
        {
            if (clip == null || sfxSources == null || sfxSources.Length == 0) return;

            AudioSource source = sfxSources[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % sfxSources.Length;
            if (source == null) return;

            source.pitch = pitchVariance > 0f ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
            source.PlayOneShot(clip);
        }

        // ------------------------------------------------------------------
        // Volume (linear 0..1 <-> mixer dB)
        // ------------------------------------------------------------------

        public void SetVolume(AudioChannel channel, float linear01)
        {
            if (mixer == null) return;
            float dB = Mathf.Log10(Mathf.Max(linear01, 0.0001f)) * 20f;
            mixer.SetFloat(ExposedParams[(int)channel], dB);
        }

        public float GetVolume(AudioChannel channel)
        {
            if (mixer == null || !mixer.GetFloat(ExposedParams[(int)channel], out float dB))
                return 1f;
            return Mathf.Pow(10f, dB / 20f);
        }

        private IEnumerator FadeToBGM(AudioClip clip, float fadeSeconds)
        {
            float startVolume = bgmSource.volume;

            for (float t = 0; t < fadeSeconds * 0.5f; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / (fadeSeconds * 0.5f));
                yield return null;
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();

            for (float t = 0; t < fadeSeconds * 0.5f; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, startVolume, t / (fadeSeconds * 0.5f));
                yield return null;
            }

            bgmSource.volume = startVolume;
            _bgmFadeRoutine = null;
        }
    }
}
