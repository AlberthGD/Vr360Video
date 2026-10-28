using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlaybackControls : MonoBehaviour
{
    [Header("Referencias")]
    public AppController appController;
    public GameObject panelVisual; 
    public Transform canvasTransform; 
    public Button playPauseButton;
    public Button backButton;
    public TMPro.TextMeshProUGUI playPauseText;
    
    [Header("Barra de Progreso")]
    public Slider barraProgreso; 
    private bool arrastrandoBarra = false; 

    [Header("Ajustes VR")]
    public float distanciaFrente = 2.0f;
    public float alturaOffset = -0.25f;

    void Awake()
    {
        playPauseButton.onClick.AddListener(OnPlayPauseClicked);
        backButton.onClick.AddListener(OnBackClicked);

        if (canvasTransform == null && panelVisual != null)
            canvasTransform = panelVisual.transform.root;

        if (barraProgreso != null)
        {
            barraProgreso.onValueChanged.AddListener(AlMoverBarra);

            EventTrigger trigger = barraProgreso.gameObject.GetComponent<EventTrigger>() ?? barraProgreso.gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { 
                CancelInvoke(nameof(PermitirActualizacionBarra)); 
                arrastrandoBarra = true; 
            });
            trigger.triggers.Add(entryDown);

            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { 
                Invoke(nameof(PermitirActualizacionBarra), 0.8f); 
            });
            trigger.triggers.Add(entryUp);
        }
    }

    private void PermitirActualizacionBarra()
    {
        arrastrandoBarra = false;
    }

    void Update()
    {
        if (appController == null) return;

        if (appController.State == AppState.Playing || appController.State == AppState.Paused)
        {
            bool botonVR = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.All) ||
                           OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.All) ||
                           OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.All);

            bool teclaEditor = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            if (botonVR || teclaEditor) TogglePanel();

            if (panelVisual != null && panelVisual.activeSelf && !arrastrandoBarra && barraProgreso != null)
            {
                var vs = appController.videoService;
                if (vs != null && vs.EstaPreparado && vs.DuracionTotal > 0)
                {
                    float progreso = (float)(vs.TiempoActual / vs.DuracionTotal);
                    barraProgreso.SetValueWithoutNotify(progreso); 
                }
            }
        }
    }

    private void AlMoverBarra(float valor)
    {
        if (appController.videoService != null)
        {
            appController.videoService.MoverTiempo(valor);
        }
    }

    public void MostrarControles()
    {
        if (panelVisual != null)
        {
            panelVisual.SetActive(true);
            ReposicionarCanvasFrenteALaMirada();
        }
    }

    public void OcultarControles()
    {
        if (panelVisual != null) panelVisual.SetActive(false);
    }

    private void TogglePanel()
    {
        if (panelVisual == null) return;
        bool mostrar = !panelVisual.activeSelf;
        panelVisual.SetActive(mostrar);
        if (mostrar) ReposicionarCanvasFrenteALaMirada();
    }

    private void ReposicionarCanvasFrenteALaMirada()
    {
        if (Camera.main == null || canvasTransform == null) return;

        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 camPos = Camera.main.transform.position;
        float alturaBase = camPos.y > 0.3f ? camPos.y : 1.4f;

        canvasTransform.position = new Vector3(camPos.x, alturaBase + alturaOffset, camPos.z) + (forward * distanciaFrente);
        canvasTransform.forward = forward;
    }

    private void OnPlayPauseClicked()
    {
        appController.TogglePause();
        if (playPauseText != null)
            playPauseText.text = appController.State == AppState.Paused ? "Reanudar" : "Pausar";
    }

    private void OnBackClicked()
    {
        appController.BackToMenu();
        if (playPauseText != null) playPauseText.text = "Pausar";
        if (barraProgreso != null) barraProgreso.SetValueWithoutNotify(0);
        OcultarControles();
    }
}