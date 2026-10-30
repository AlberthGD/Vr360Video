using System.IO;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoService : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer videoPlayer;
    public RenderTexture videoRenderTexture;
    public AppController appController;

    public double TiempoActual => videoPlayer.time;
    public double DuracionTotal => videoPlayer.length;
    public bool EstaPreparado => videoPlayer.isPrepared;

    private VideoEntry videoActual;
    public VideoEntry VideoActual => videoActual;

    void Awake()
    {
        if (videoPlayer == null) 
            videoPlayer = GetComponent<VideoPlayer>();
        
        videoPlayer.prepareCompleted += OnPrepareCompleted;
        videoPlayer.loopPointReached += OnLoopPointReached;
        videoPlayer.errorReceived += OnVideoError;
    }

    // Si un video remoto no carga (sin red, URL mala) se regresa al menú en lugar de quedarse en "Cargando"
    private void OnVideoError(VideoPlayer vp, string mensaje)
    {
        Debug.LogError($"[Video] Error al reproducir {videoActual?.title}: {mensaje}");
        appController.BackToMenu();
    }

    // adelantar o rebobinar video con el slider
    public void MoverTiempo(float porcentaje)
    {
        if (videoPlayer.isPrepared)
        {
            videoPlayer.time = porcentaje * videoPlayer.length;
        }
    }

    // Cargando .mp4
    public void CargarVideo(VideoEntry entry)
    {
        videoActual = entry;
        
        if (videoRenderTexture != null)
        {
            RenderTexture.active = videoRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        // Si el catálogo trae una URL remota se reproduce por streaming; si no, desde StreamingAssets
        string path = !string.IsNullOrEmpty(entry.url)
            ? entry.url
            : Path.Combine(Application.streamingAssetsPath, entry.fileName);
        videoPlayer.url = path;
        videoPlayer.isLooping = entry.loop;

        videoPlayer.Prepare(); 
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        Debug.Log($"[Video] {videoActual?.title}: {vp.width}x{vp.height}, estéreo = {videoActual?.stereo}");
        appController.OnVideoPrepared();
    }

    // Estados del video
    public void Reproducir()
    {
        videoPlayer.Play();
    }

    public void Pausar()
    {
        videoPlayer.Pause();
    }

    public void Reanudar()
    {
        videoPlayer.Play();
    }

    public void Detener()
    {
        videoPlayer.Stop();
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        if (!videoPlayer.isLooping)
        {
            appController.OnVideoFinished();
        }
    }
}