using UnityEngine;
using FMODUnity;

public class AudioController : MonoBehaviour
{
    [SerializeField] StudioEventEmitter bckgMusicEventEmitter;
    [SerializeField] StudioEventEmitter sfxEventEmitter;
    
    public void PlayPauseMusic()
    {
        if (bckgMusicEventEmitter.IsPlaying())
        {
            bckgMusicEventEmitter.Stop();
        }
        else
        {
            bckgMusicEventEmitter.Play();
        }
    }

    public void PlaySfx()
    {
        FMODUnity.RuntimeManager.PlayOneShot(sfxEventEmitter.EventReference);
    }
}
