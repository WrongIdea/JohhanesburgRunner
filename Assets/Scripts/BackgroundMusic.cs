using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Looping background music for the whole session — menu, gameplay,
    /// pause, game over — starting the instant the scene loads and never
    /// stopping. Global mute is AudioListener.volume (see SoundSettings),
    /// which this mixes through like every other sound.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {
        void Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            source.loop = true;
            source.Play();
        }
    }
}
