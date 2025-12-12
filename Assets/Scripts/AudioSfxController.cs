using UnityEngine;
using FMODUnity;

public class AudioSfxController : MonoBehaviour
{
    [SerializeField] StudioEventEmitter sfxEventEmitter;

    public void PlaySfx()
    {
        FMODUnity.RuntimeManager.PlayOneShot(sfxEventEmitter.EventReference);
    }
}
