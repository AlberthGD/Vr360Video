using System.Collections.Generic;
using UnityEngine;

// Punto de interés dentro del video 360 (coordenadas esféricas)
[System.Serializable]
public class HotspotData
{
    public string title;        // Título que se muestra en el panel
    [TextArea] public string description; // Información al seleccionarlo
    public float yaw;           // Grados horizontales. 0 = centro del video, + derecha, - izquierda (-180..180)
    public float pitch;         // Grados verticales. 0 = horizonte, + arriba, - abajo (-90..90)
    public float startTime;     // Segundo en que aparece
    public float endTime = -1f; // Segundo en que desaparece (-1 = hasta el final)
}

[System.Serializable]
public class VideoEntry
{
    public string title; // Nombre del video
    public string fileName; // Nombre del archivo de video StreamingAssets
    public string url; // (Opcional) URL remota del video. Si existe, tiene prioridad sobre fileName
    public Sprite thumbnail; // Icono
    public string thumbnailUrl; // (Opcional) URL de la miniatura cuando se usa el catálogo JSON
    public string duration; // Duracion xd
    public bool loop; // Si se repite
    public string stereo = "Mono"; // "Mono", "OverUnder" (arriba/abajo) o "SideBySide"
    public List<HotspotData> hotspots = new();

    // 0 = Mono, 1 = Side by Side, 2 = Over Under (valores de _Layout del shader Skybox/Panoramic)
    public int StereoLayout
    {
        get
        {
            string s = (stereo ?? "").Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            if (s == "overunder" || s == "ou" || s == "topbottom" || s == "tb") return 2;
            if (s == "sidebyside" || s == "sbs" || s == "lr") return 1;
            return 0;
        }
    }
}

// Estructura raíz del JSON descargado del servidor
[System.Serializable]
public class CatalogData
{
    public int version;
    public List<VideoEntry> videos = new();
}

[CreateAssetMenu(menuName = "Video360/Catalog")]
public class VideoCatalog : ScriptableObject
{
    public List<VideoEntry> videos = new();
}
