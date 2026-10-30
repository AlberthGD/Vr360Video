using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Descarga el catálogo de videos (JSON) desde un servidor para poder agregar
// videos sin recompilar la aplicación.
// Orden de respaldo: Servidor -> Copia en caché -> StreamingAssets/catalog.json -> ScriptableObject
public class RemoteCatalogLoader : MonoBehaviour
{
    [Header("Servidor")]
    [Tooltip("URL del catalog.json (GitHub raw, GitHub Pages o un servidor local: http://IP_PC:8000/catalog.json)")]
    public string catalogUrl = "https://raw.githubusercontent.com/AlberthGD/Vr360Video/main/Server/catalog.json";
    public int timeoutSegundos = 8;
    [Tooltip("Vuelve a descargar el catálogo cada vez que se regresa al menú")]
    public bool recargarAlVolverAlMenu = true;

    [Header("Referencias")]
    public MenuView menuView;
    public VideoCatalog catalogoLocal; // Respaldo final y fuente de miniaturas locales
    public TMPro.TextMeshProUGUI statusText; // (Opcional) "Descargando catálogo..."

    public List<VideoEntry> VideosActuales { get; private set; } = new();
    public bool Cargando { get; private set; }

    private readonly Dictionary<string, Sprite> cacheMiniaturas = new();
    private string RutaCache => Path.Combine(Application.persistentDataPath, "catalog_cache.json");

    void Start()
    {
        Recargar();
    }

    public void Recargar()
    {
        if (Cargando) return;
        StartCoroutine(RutinaCargar());
    }

    private IEnumerator RutinaCargar()
    {
        Cargando = true;
        MostrarEstado("Descargando catálogo...");

        string json = null;
        string origen = "";

        // 1) Servidor
        if (!string.IsNullOrEmpty(catalogUrl))
        {
            // Parámetro anti-caché para que siempre se lea la última versión
            string url = catalogUrl + (catalogUrl.Contains("?") ? "&" : "?") + "t=" + System.DateTime.UtcNow.Ticks;
            using var req = UnityWebRequest.Get(url);
            req.timeout = timeoutSegundos;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && EsCatalogoValido(req.downloadHandler.text))
            {
                json = req.downloadHandler.text;
                origen = "servidor";
                try { File.WriteAllText(RutaCache, json); } catch { /* sin caché, no es crítico */ }
            }
            else
            {
                Debug.LogWarning($"[Catalogo] No se pudo descargar ({req.error}). Usando respaldo.");
            }
        }

        // 2) Caché de la última descarga exitosa
        if (json == null && File.Exists(RutaCache))
        {
            string cache = File.ReadAllText(RutaCache);
            if (EsCatalogoValido(cache)) { json = cache; origen = "caché"; }
        }

        // 3) StreamingAssets/catalog.json (en Android se lee con UnityWebRequest)
        if (json == null)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, "catalog.json");
            if (!ruta.Contains("://")) ruta = "file://" + ruta;
            using var req = UnityWebRequest.Get(ruta);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && EsCatalogoValido(req.downloadHandler.text))
            {
                json = req.downloadHandler.text;
                origen = "local";
            }
        }

        List<VideoEntry> videos;
        if (json != null)
        {
            videos = JsonUtility.FromJson<CatalogData>(json).videos;
        }
        else
        {
            // 4) ScriptableObject original
            videos = catalogoLocal != null ? catalogoLocal.videos : new List<VideoEntry>();
            origen = "ScriptableObject";
        }

        // Miniaturas: URL remota o, si falla, el Sprite del ScriptableObject con el mismo archivo
        foreach (var v in videos)
        {
            if (v.hotspots == null) v.hotspots = new List<HotspotData>();
            if (v.thumbnail != null) continue;

            if (!string.IsNullOrEmpty(v.thumbnailUrl))
                yield return DescargarMiniatura(v);

            if (v.thumbnail == null) v.thumbnail = BuscarSpriteLocal(v);
        }

        VideosActuales = videos;
        Debug.Log($"[Catalogo] {videos.Count} videos cargados desde {origen}.");
        MostrarEstado(null);
        Cargando = false;

        if (menuView != null) menuView.ConstruirMenu(VideosActuales);
    }

    private IEnumerator DescargarMiniatura(VideoEntry v)
    {
        if (cacheMiniaturas.TryGetValue(v.thumbnailUrl, out var spr)) { v.thumbnail = spr; yield break; }

        using var req = UnityWebRequestTexture.GetTexture(v.thumbnailUrl);
        req.timeout = timeoutSegundos;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var tex = DownloadHandlerTexture.GetContent(req);
            spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            cacheMiniaturas[v.thumbnailUrl] = spr;
            v.thumbnail = spr;
        }
    }

    private Sprite BuscarSpriteLocal(VideoEntry v)
    {
        if (catalogoLocal == null) return null;
        foreach (var local in catalogoLocal.videos)
            if (local.fileName == v.fileName || local.title == v.title) return local.thumbnail;
        return null;
    }

    private static bool EsCatalogoValido(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var data = JsonUtility.FromJson<CatalogData>(json);
            return data != null && data.videos != null && data.videos.Count > 0;
        }
        catch { return false; }
    }

    private void MostrarEstado(string msg)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        statusText.text = msg ?? "";
    }
}
