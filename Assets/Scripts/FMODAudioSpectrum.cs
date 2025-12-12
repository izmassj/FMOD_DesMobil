using System;
using UnityEngine;
using System.Runtime.InteropServices;
using FMODUnity;

class ScriptUsageLowLevel : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] string VCAPath = "vca:/General"; 
    [SerializeField] float WIDTH = -20.0f;
    [SerializeField] float HEIGHT = 0.2f;

    FMOD.DSP fft;
    FMOD.Studio.VCA vca;
    FMOD.ChannelGroup vcaChannelGroup;

    const int WindowSize = 1024;


    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
            {
                Debug.LogError("No LineRenderer assigned or found on GameObject!");
                return;
            }
        }

        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.positionCount = WindowSize;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.getVCA(VCAPath, out vca);

        if (result != FMOD.RESULT.OK || !vca.isValid())
        {
            Debug.LogError($"Failed to get VCA at path: {VCAPath}. Error: {result}");
            return;
        }

        result = FMODUnity.RuntimeManager.CoreSystem.createDSPByType(
            FMOD.DSP_TYPE.FFT,
            out fft
        );

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to create FFT DSP: {result}");
            return;
        }

        fft.setParameterInt((int)FMOD.DSP_FFT_WINDOW_TYPE.RECT, (int)FMOD.DSP_FFT_WINDOW_TYPE.HAMMING);
        fft.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, WindowSize * 2);

        FMOD.Studio.Bus vcaBus;
        result = FMODUnity.RuntimeManager.StudioSystem.getBus(VCAPath, out vcaBus);

        if (result == FMOD.RESULT.OK && vcaBus.isValid())
        {
            FMOD.ChannelGroup busPtr;
            vcaBus.getChannelGroup(out busPtr);

            vcaChannelGroup = new FMOD.ChannelGroup();

            vcaChannelGroup.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, fft);
            Debug.Log($"FFT DSP added to VCA: {VCAPath}");

        }
        else
        {
            Debug.LogWarning($"Could not get bus for VCA: {VCAPath}. Adding to master channel group.");
            AddToMasterAsFallback();
        }
    }

    void AddToMasterAsFallback()
    {
        FMOD.RESULT result = FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out vcaChannelGroup);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to get master channel group: {result}");
            return;
        }

        vcaChannelGroup.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, fft);
        Debug.Log("FFT DSP added to master channel group as fallback");
    }

    void Update()
    {
        if (!fft.hasHandle())
        {
            Debug.LogWarning("FFT handle not valid");
            return;
        }

        if (vca.isValid())
        {
            float vcaVolume;
            FMOD.RESULT result1 = vca.getVolume(out vcaVolume);

            if (result1 == FMOD.RESULT.OK && vcaVolume <= 0.001f)
            {
                DrawFlatLine();
                return;
            }
        }

        IntPtr unmanagedData;
        uint length;

        // Obtener datos del parámetro FFT
        FMOD.RESULT result = fft.getParameterData(
            (int)FMOD.DSP_FFT.SPECTRUMDATA,
            out unmanagedData,
            out length
        );

        if (result != FMOD.RESULT.OK || unmanagedData == IntPtr.Zero)
        {
            DrawFlatLine();
            return;
        }

        try
        {
            FMOD.DSP_PARAMETER_FFT fftData = (FMOD.DSP_PARAMETER_FFT)Marshal.PtrToStructure(
                unmanagedData,
                typeof(FMOD.DSP_PARAMETER_FFT)
            );

            // Validar datos
            if (fftData.numchannels <= 0 || fftData.spectrum == null || fftData.spectrum.Length == 0)
            {
                DrawFlatLine();
                return;
            }

            var spectrum = fftData.spectrum;

            Vector3[] positions = new Vector3[WindowSize];
            float xStep = WIDTH / WindowSize;
            float startX = -WIDTH * 0.5f;

            Vector3 objectPosition = transform.position;
            Quaternion objectRotation = transform.rotation;
            Vector3 objectScale = transform.lossyScale;

            float vcaVolume = 1.0f;
            float vcaFaderLevel = 0.0f;

            if (vca.isValid())
            {
                vca.getVolume(out vcaVolume);
            }

            for (int i = 0; i < WindowSize; ++i)
            {
                float value = spectrum[0][Mathf.Min(i, spectrum[0].Length - 1)];

                float volumeAdjustment = vcaFaderLevel > 0.001f ? vcaFaderLevel : vcaVolume;
                value *= volumeAdjustment;

                float level = Lin2dB(value);

                Vector3 localPosition = new Vector3(
                    startX + (i * xStep),
                    Mathf.Max((80 + level) * HEIGHT, 0) * objectScale.y,
                    0
                );

                localPosition = Vector3.Scale(localPosition, objectScale);
                positions[i] = objectPosition + objectRotation * localPosition;
            }

            lineRenderer.SetPositions(positions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing FFT data: {ex.Message}");
            DrawFlatLine();
        }
    }

    void DrawFlatLine()
    {
        Vector3[] positions = new Vector3[WindowSize];
        float xStep = WIDTH / WindowSize;
        float startX = -WIDTH * 0.5f;

        Vector3 objectPosition = transform.position;
        Quaternion objectRotation = transform.rotation;
        Vector3 objectScale = transform.lossyScale;

        for (int i = 0; i < WindowSize; ++i)
        {
            Vector3 localPosition = new Vector3(
                startX + (i * xStep),
                0,
                0
            );

            localPosition = Vector3.Scale(localPosition, objectScale);
            positions[i] = objectPosition + objectRotation * localPosition;
        }

        lineRenderer.SetPositions(positions);
    }

    float Lin2dB(float linear)
    {
        if (linear <= 0.000001f)
            return -80.0f;

        float dbValue = Mathf.Log10(linear) * 20.0f;
        return Mathf.Clamp(dbValue, -80.0f, 0.0f);
    }

    void OnDestroy()
    {
        // Limpiar recursos
        if (vcaChannelGroup.hasHandle() && fft.hasHandle())
        {
            vcaChannelGroup.removeDSP(fft);
        }

        if (fft.hasHandle())
        {
            fft.release();
        }
    }

    public void SetVCAVolume(float volume)
    {
        if (vca.isValid())
        {
            vca.setVolume(Mathf.Clamp01(volume));
        }
    }

    public float GetVCAVolume()
    {
        if (vca.isValid())
        {
            float volume;
            vca.getVolume(out volume);
            return volume;
        }
        return 0f;
    }
}