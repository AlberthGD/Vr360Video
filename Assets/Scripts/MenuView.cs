using System.Collections;
using UnityEngine;

public class MenuView : MonoBehaviour
{
    [Header("Referencias")]
    public VideoCatalog catalog;
    public GameObject videoButtonPrefab;
    public Transform buttonContainer; // Panel donde se guardan los videoButtonPrefab
    public AppController appController;
    public Transform canvasTransform; 

    void Start()
    {
        if (canvasTransform == null && buttonContainer != null)
            canvasTransform = buttonContainer.root;

        GenerarBotones();
    }

    public void MostrarMenu()
    {
        if (canvasTransform != null) canvasTransform.gameObject.SetActive(true);        
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(ReposicionarConRetraso());
    }

    public void OcultarMenu()
    {
        if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
    }

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
        ReposicionarFrenteAlUsuario();
    }

    private void ReposicionarFrenteAlUsuario()
    {
        if (Camera.main == null) return;

        Transform objetivo = canvasTransform != null ? canvasTransform : transform;

        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 camPos = Camera.main.transform.position;
        
        // toma altura de la camra o 1.4m si en el editor sigue en 0
        float alturaMenu = camPos.y > 0.3f ? camPos.y : 1.4f;

        // coloca el canvas a 2 m frente al usuario
        objetivo.position = new Vector3(camPos.x, alturaMenu, camPos.z) + (forward * 2.0f);
        objetivo.forward = forward;
    }
}