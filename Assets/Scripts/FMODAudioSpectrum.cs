using System;
using UnityEngine;
using System.Runtime.InteropServices;
using FMODUnity;

class ScriptUsageLowLevel : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] string VCAPath = "vca:/General"; // Ruta del VCA que quieres visualizar

    FMOD.DSP fft;
    FMOD.Studio.VCA vca;
    FMOD.ChannelGroup vcaChannelGroup;

    const int WindowSize = 1024;
    const float WIDTH = -10.0f;
    const float HEIGHT = 0.1f;

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

        // Setup LineRenderer
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.positionCount = WindowSize;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        // Obtener el VCA
        FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.getVCA(VCAPath, out vca);

        if (result != FMOD.RESULT.OK || !vca.isValid())
        {
            Debug.LogError($"Failed to get VCA at path: {VCAPath}. Error: {result}");
            return;
        }

        // Crear y configurar FFT DSP
        result = FMODUnity.RuntimeManager.CoreSystem.createDSPByType(
            FMOD.DSP_TYPE.FFT,
            out fft
        );

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to create FFT DSP: {result}");
            return;
        }

        // Configurar parámetros FFT
        fft.setParameterInt((int)FMOD.DSP_FFT_WINDOW_TYPE.RECT, (int)FMOD.DSP_FFT_WINDOW_TYPE.HAMMING);
        fft.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, WindowSize * 2);

        // Encontrar y conectar al ChannelGroup del VCA
        // Nota: FMOD no proporciona acceso directo al ChannelGroup del VCA,
        // así que necesitamos un enfoque diferente

        // Opción 1: Añadir el DSP al bus asociado al VCA
        // Primero necesitamos obtener el bus del VCA
        FMOD.Studio.Bus vcaBus;
        result = FMODUnity.RuntimeManager.StudioSystem.getBus(VCAPath, out vcaBus);

        if (result == FMOD.RESULT.OK && vcaBus.isValid())
        {
            // Obtener el ChannelGroup del bus
            FMOD.ChannelGroup busPtr;
            vcaBus.getChannelGroup(out busPtr);

            vcaChannelGroup = new FMOD.ChannelGroup();

            // Añadir el DSP al grupo de canales del VCA
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
        // Fallback: añadir al grupo de canales maestro
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
        // Verificar si FFT es válido
        if (!fft.hasHandle())
        {
            Debug.LogWarning("FFT handle not valid");
            return;
        }

        // Verificar si el VCA está silenciado
        if (vca.isValid())
        {
            float vcaVolume;
            FMOD.RESULT result1 = vca.getVolume(out vcaVolume);

            if (result1 == FMOD.RESULT.OK && vcaVolume <= 0.001f)
            {
                // El VCA está silenciado, mostrar línea plana
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
            // Marshal los datos
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

            // Crear array de posiciones
            Vector3[] positions = new Vector3[WindowSize];
            float xStep = WIDTH / WindowSize;
            float startX = -WIDTH * 0.5f;

            // Obtener la transformación del objeto
            Vector3 objectPosition = transform.position;
            Quaternion objectRotation = transform.rotation;
            Vector3 objectScale = transform.lossyScale;

            // Obtener volumen del VCA para ajustar la visualización
            float vcaVolume = 1.0f;
            float vcaFaderLevel = 0.0f;

            if (vca.isValid())
            {
                vca.getVolume(out vcaVolume);
            }

            for (int i = 0; i < WindowSize; ++i)
            {
                // Usar datos del espectro del primer canal
                float value = spectrum[0][Mathf.Min(i, spectrum[0].Length - 1)];

                // Ajustar por volumen y fader level del VCA
                // Usar fader level si está disponible, de lo contrario usar volumen
                float volumeAdjustment = vcaFaderLevel > 0.001f ? vcaFaderLevel : vcaVolume;
                value *= volumeAdjustment;

                // Convertir a dB con verificación de seguridad
                float level = Lin2dB(value);

                // Calcular posición local relativa al objeto
                Vector3 localPosition = new Vector3(
                    startX + (i * xStep),
                    Mathf.Max((80 + level) * HEIGHT, 0) * objectScale.y,
                    0
                );

                // Aplicar rotación y posición del objeto
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

    // Métodos públicos para control desde otros scripts

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