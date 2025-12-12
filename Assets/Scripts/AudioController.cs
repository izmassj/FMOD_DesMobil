using UnityEngine;
using FMODUnity;

public class AudioController : MonoBehaviour
{
    [SerializeField] StudioEventEmitter bckgMusicEventEmitter;

    public void PlayPauseMusic()
    {
        if (bckgMusicEventEmitter.EventInstance.getPaused(out bool paused) == FMOD.RESULT.OK)
        {
            bckgMusicEventEmitter.EventInstance.setPaused(!paused);
        }
    }
}
