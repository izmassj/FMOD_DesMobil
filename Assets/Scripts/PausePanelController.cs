using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PausePanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private LineRenderer visualizer;

    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("VCA Paths (FMOD)")]
    private string masterVCAPath = "vca:/General";
    private string musicVCAPath = "vca:/Music";
    private string sfxVCAPath = "vca:/Sfx";

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Audio")]
    [SerializeField] StudioEventEmitter bckgMusicEventEmitter;

    private bool isPaused = false;
    private InputAction pauseAction;
    private VCA masterVCA;
    private VCA musicVCA;
    private VCA sfxVCA;

    void Start()
    {
        pauseAction = inputActions.FindAction("Pause");

        if (pausePanel != null)
            pausePanel.SetActive(false);

        pauseAction.performed += OnPauseInput;

        // Inicializar VCAs de FMOD
        InitializeFMODAudio();

        // Configurar sliders
        SetupSliders();
    }

    void InitializeFMODAudio()
    {
        // Obtener los VCAs de FMOD
        masterVCA = RuntimeManager.GetVCA(masterVCAPath);
        musicVCA = RuntimeManager.GetVCA(musicVCAPath);
        sfxVCA = RuntimeManager.GetVCA(sfxVCAPath);
    }

    void SetupSliders()
    {
        if (masterVolumeSlider != null)
        {
            if (masterVCA.isValid())
            {
                masterVCA.getVolume(out float currentMasterVolume);
                masterVolumeSlider.value = currentMasterVolume;
            }
            else
            {
                Debug.LogWarning($"Master VCA not found at path: {masterVCAPath}");
                masterVolumeSlider.value = 1f;
            }

            masterVolumeSlider.onValueChanged.AddListener(SetGeneralVolume);
        }

        if (musicVolumeSlider != null)
        {
            if (musicVCA.isValid())
            {
                musicVCA.getVolume(out float currentMusicVolume); 
                musicVolumeSlider.value = currentMusicVolume;
            }
            else
            {
                Debug.LogWarning($"Music VCA not found at path: {musicVCAPath}");
                musicVolumeSlider.value = 1f;
            }

            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            if (sfxVCA.isValid())
            {
                sfxVCA.getVolume(out float currentSfxVolume); 
                sfxVolumeSlider.value = currentSfxVolume;
            }
            else
            {
                Debug.LogWarning($"SFX VCA not found at path: {sfxVCAPath}");
                sfxVolumeSlider.value = 1f;
            }

            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void SetGeneralVolume(float volume)
    {
        if (masterVCA.isValid())
        {
            masterVCA.setVolume(volume);
            Debug.Log($"Master Volume set to: {volume}");
        }
        else
        {
            Debug.LogWarning($"Master VCA not valid at path: {masterVCAPath}");
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicVCA.isValid())
        {
            musicVCA.setVolume(volume); 
            Debug.Log($"Music Volume set to: {volume}");
        }
        else
        {
            Debug.LogWarning($"Music VCA not valid at path: {musicVCAPath}");
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxVCA.isValid())
        {
            sfxVCA.setVolume(volume); 
            Debug.Log($"SFX Volume set to: {volume}");
        }
        else
        {
            Debug.LogWarning($"SFX VCA not valid at path: {sfxVCAPath}");
        }
    }

    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
            bckgMusicEventEmitter.EventInstance.setPaused(false);
        }
        else
        {
            PauseGame();
            bckgMusicEventEmitter.EventInstance.setPaused(true);
        }
    }

    void PauseGame()
    {
        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            if (visualizer != null)
                visualizer.enabled = false;
        }
    }

    void ResumeGame()
    {
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            if (visualizer != null)
                visualizer.enabled = true;
        }
    }

    void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.Enable();
    }

    void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPauseInput;
            pauseAction.Disable();
        }

        // Limpiar listeners de sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetGeneralVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }
}