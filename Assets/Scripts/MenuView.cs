using System.Collections;
using UnityEngine;

public class MenuView : MonoBehaviour
{
    [Header("Referencias")]
    public VideoCatalog catalog; // Video/Catalogo
    public GameObject videoButtonPrefab; // Prefab de los botones
    public Transform buttonContainer; // Panel donde se guardan los videoButtonPrefab
    public AppController appController;
    public Transform canvasTransform; 

    void Start()
    {
        if (canvasTransform == null && buttonContainer != null)
            canvasTransform = buttonContainer.root;

        GenerarBotones();
    }

    // Mostrar/Ocultar
    public void MostrarMenu()
    {
        // Mostar contenerlo principal donde apararecen los videoButtonPrefab
        if (canvasTransform != null) canvasTransform.gameObject.SetActive(true);        
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(ReposicionarConRetraso());
    }

    public void OcultarMenu()
    {
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
    }

    // Generan los videoButtonPrefab dentro del buttonContainer
    private void GenerarBotones()
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        foreach (var video in catalog.videos)
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