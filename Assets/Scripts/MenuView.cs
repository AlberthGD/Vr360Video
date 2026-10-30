using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuView : MonoBehaviour
{
    [Header("Referencias")]
    public VideoCatalog catalog; // Video/Catalogo (respaldo si no hay catálogo remoto)
    public RemoteCatalogLoader catalogLoader; // (Opcional) Catálogo JSON descargado del servidor
    public GameObject videoButtonPrefab; // Prefab de los botones
    public Transform buttonContainer; // Panel donde se guardan los videoButtonPrefab
    public AppController appController;
    public Transform canvasTransform; 

    private bool menuMostradoAntes = false;

    void Start()
    {
        if (canvasTransform == null && buttonContainer != null)
            canvasTransform = buttonContainer.root;

        // Si hay cargador remoto, él construye el menú cuando termina la descarga
        if (catalogLoader == null && catalog != null)
            ConstruirMenu(catalog.videos);
    }

    // Mostrar/Ocultar
    public void MostrarMenu()
    {
        // Mostar contenerlo principal donde apararecen los videoButtonPrefab
        if (canvasTransform != null) canvasTransform.gameObject.SetActive(true);        
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);

        // Al volver al menú se vuelve a leer el JSON: los videos nuevos aparecen sin recompilar
        if (menuMostradoAntes && catalogLoader != null && catalogLoader.recargarAlVolverAlMenu)
            catalogLoader.Recargar();
        menuMostradoAntes = true;

        StopAllCoroutines();
        StartCoroutine(ReposicionarConRetraso());
    }

    public void OcultarMenu()
    {
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
    }

    // Generan los videoButtonPrefab dentro del buttonContainer
    public void ConstruirMenu(List<VideoEntry> videos)
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        foreach (var video in videos)
        {
            GameObject btnObj = Instantiate(videoButtonPrefab, buttonContainer);            
            btnObj.transform.localScale = Vector3.one;
            Vector3 localPos = btnObj.transform.localPosition;
            btnObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            VideoButton btn = btnObj.GetComponent<VideoButton>();
            btn.Configurar(video, appController);
        }
    }

    private IEnumerator ReposicionarConRetraso()
    {
        yield return new WaitForSeconds(0.1f);
    }
}
