using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Centralized audio for Debug Duo. Owns a music AudioSource (looping) and an
    /// SFX AudioSource (one-shots via PlayOneShot). All clip slots are exposed in
    /// the Inspector — drop in audio assets when you have them; with no clips set,
    /// the calls are no-ops and the game still runs silently.
    ///
    /// Usage: AudioManager.Instance?.PlayStep(); etc.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources (auto-created if left empty)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [Tooltip("Looping background music. Drop a low-volume music track here.")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;

        [Header("SFX Clips")]
        [Tooltip("Plays when a player lands after Walk / Run / Walk Back.")]
        [SerializeField] private AudioClip stepClip;

        [Tooltip("Plays on Turn Left / Turn Right.")]
        [SerializeField] private AudioClip turnClip;

        [Tooltip("Plays when any UI button is clicked.")]
        [SerializeField] private AudioClip clickClip;

        [Tooltip("Plays when both players reach their goals.")]
        [SerializeField] private AudioClip successClip;

        [Tooltip("Plays when movement is blocked or a wrong answer is selected.")]
        [SerializeField] private AudioClip failClip;

        [Header("SFX Volumes")]
        [SerializeField, Range(0f, 1f)] private float stepVolume    = 0.55f;
        [SerializeField, Range(0f, 1f)] private float turnVolume    = 0.45f;
        [SerializeField, Range(0f, 1f)] private float clickVolume   = 0.40f;
        [SerializeField, Range(0f, 1f)] private float successVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float failVolume    = 0.55f;

        private static AudioManager instance;
        public static AudioManager Instance => instance;

        private void Awake()
        {
            instance = this;
            EnsureSources();
            StartMusic();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                var go = new GameObject("MusicSource");
                go.transform.SetParent(transform, false);
                musicSource = go.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D
            }
            musicSource.volume = musicVolume;

            if (sfxSource == null)
            {
                var go = new GameObject("SFXSource");
                go.transform.SetParent(transform, false);
                sfxSource = go.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }
        }

        private void StartMusic()
        {
            if (musicClip == null || musicSource == null) return;
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void PlayStep()    => PlayClip(stepClip,    stepVolume);
        public void PlayTurn()    => PlayClip(turnClip,    turnVolume);
        public void PlayClick()   => PlayClip(clickClip,   clickVolume);
        public void PlaySuccess() => PlayClip(successClip, successVolume);
        public void PlayFail()    => PlayClip(failClip,    failVolume);

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
