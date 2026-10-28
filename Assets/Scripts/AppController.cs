using System.Collections;
using UnityEngine;

public enum AppState { Menu, Loading, Playing, Paused }

public class AppController : MonoBehaviour
{
    public AppState State { get; private set; }

    [Header("UI")]
    public MenuView menuView;
    public VideoService videoService;
    public PlaybackControls playbackControls;
    public GameObject loadingUI;

    [Header("Efecto de Fundido")]
    public CanvasGroup fadeGroup; // Para tapar la vista
    public float fadeDuration = 0.5f;  

    [Header("Entorno")]
    public Material menuSkybox;
    public Material videoSkybox;

    void Start()
    {
        if (fadeGroup != null) fadeGroup.alpha = 0f; 
        CambiarEstado(AppState.Menu);
    }

    public void SelectVideo(VideoEntry entry)
    {
        if (State == AppState.Menu)
        {
            StartCoroutine(RutinaCargarVideo(entry));
        }
    }

    private IEnumerator RutinaCargarVideo(VideoEntry entry)
    {
        // Inicio
        yield return StartCoroutine(Fade(1f));

        // Cambiamos a pantalla de carga
        CambiarEstado(AppState.Loading);
        videoService.CargarVideo(entry);

        // Volvemos a transparente 
        yield return StartCoroutine(Fade(0f));
    }

    public void OnVideoPrepared()
    {
        StartCoroutine(RutinaMostrarVideo());
    }

    private IEnumerator RutinaMostrarVideo()
    {
        // Fundido a negro 
        yield return StartCoroutine(Fade(1f));

        // Activamops el video
        CambiarEstado(AppState.Playing);
        videoService.Reproducir();

        // Volvemos a transparente 
        yield return StartCoroutine(Fade(0f));
    }

    public void TogglePause()
    {
        if (State == AppState.Playing)
        {
            CambiarEstado(AppState.Paused);
            videoService.Pausar();
        }
        else if (State == AppState.Paused)
        {
            CambiarEstado(AppState.Playing);
            videoService.Reanudar();
        }
    }

    public void BackToMenu()
    {
        StartCoroutine(RutinaVolverAlMenu());
    }

    private IEnumerator RutinaVolverAlMenu()
    {
        yield return StartCoroutine(Fade(1f));
        CambiarEstado(AppState.Menu);
        videoService.Detener();
        yield return StartCoroutine(Fade(0f));
    }

    public void OnVideoFinished()
    {
        StartCoroutine(RutinaVolverAlMenu());
    }

    // Fundido a negro
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeGroup == null) yield break;

        float startAlpha = fadeGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = targetAlpha;
    }

    // Maquina de estados
    private void CambiarEstado(AppState nuevoEstado)
    {
        State = nuevoEstado;
        Camera cam = Camera.main;

        switch (State)
        {
            case AppState.Menu:
                menuView.MostrarMenu();
                if (playbackControls != null) playbackControls.OcultarControles();
                if (loadingUI != null) loadingUI.SetActive(false);
                
                if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;
                RenderSettings.skybox = null; 
                break;
                
            case AppState.Loading:
                menuView.OcultarMenu();
                if (playbackControls != null) playbackControls.OcultarControles();
                if (loadingUI != null) loadingUI.SetActive(true);
                
                if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;
                RenderSettings.skybox = null; 
                break;
                
            case AppState.Playing:
            case AppState.Paused:
                menuView.OcultarMenu();
                if (loadingUI != null) loadingUI.SetActive(false);
                
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.skybox = videoSkybox; 
                break;
        }
    }
}