using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VideoEntry
{
    public string title; // Nombre del video
    public string fileName; // Nombre del archivo de video StreamingAssets
    public Sprite thumbnail; // Icono
    public string duration; 
    public bool loop; 
}

[CreateAssetMenu(menuName = "Video360/Catalog")]
public class VideoCatalog : ScriptableObject
{
    public List<VideoEntry> videos = new();
}