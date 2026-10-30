using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlaybackControls : MonoBehaviour
{
    [Header("Referencias")]
    public AppController appController; //Script controlador 
    public GameObject panelVisual; // Root de los botones (pausa, volver) y de la timeline
    public Transform canvasTransform; // Transform del canvas para posicional
    public Button playPauseButton; //Boton para alternar play o pausa
    public Button backButton; // Boton que detiene el video y vuelve al inicio 
    public TMPro.TextMeshProUGUI playPauseText; // Texto que dice pausar o reanudar del boton de pausa
    
    // TImeline
    [Header("Barra de Progreso")]
    public Slider barraProgreso; 
    private bool arrastrandoBarra = false; 

    [Header("Ajustes VR")]
    public float distanciaFrente = 2.0f;
    public float alturaOffset = -0.25f;

    void Awake()
    {
        // Enlaza las funciones
        playPauseButton.onClick.AddListener(OnPlayPauseClicked);
        backButton.onClick.AddListener(OnBackClicked);

        if (canvasTransform == null && panelVisual != null)
            canvasTransform = panelVisual.transform.root;

        // Configuración de eventos para la barra de progreso
        if (barraProgreso != null)
        {
            // Cuando el valor de la barra cambia manualmente
            barraProgreso.onValueChanged.AddListener(AlMoverBarra);
            EventTrigger trigger = barraProgreso.gameObject.GetComponent<EventTrigger>() ?? barraProgreso.gameObject.AddComponent<EventTrigger>();
            
            //El usuario presiona el gatillo sobre la barra
            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { 
                CancelInvoke(nameof(PermitirActualizacionBarra)); // Cancela cualquier temporizador de reactivación 
                arrastrandoBarra = true; // Pausa la actualización automática
            });
            trigger.triggers.Add(entryDown);

            // El usuario suelta el gatillo
            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { 
                // Espera 0.8 segundos antes de volver a sincronizar la barra con el video
                Invoke(nameof(PermitirActualizacionBarra), 0.8f); 
            });
            trigger.triggers.Add(entryUp);
        }
    }

    // Reactiva la sincronizacion del slider 
    private void PermitirActualizacionBarra()
    {
        arrastrandoBarra = false;
    }

    void Update()
    {
        if (appController == null) return;

        if (appController.State == AppState.Playing || appController.State == AppState.Paused)
        {
            // Detecta si se presiona A, B, X, Y o el botón Menú en los mandos de Meta Quest
            bool botonVR = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.All) ||
                           OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.All) ||
                           OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.All);

            bool teclaEditor = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            // Si se presiona algún botón asignado, alterna la visibilidad del menu
            if (botonVR || teclaEditor) TogglePanel();

            // Sincronizacion Si el panel está visible y no  esta arrastrando la barra manualmente
            if (panelVisual != null && panelVisual.activeSelf && !arrastrandoBarra && barraProgreso != null)
            {
                var vs = appController.videoService;
                if (vs != null && vs.EstaPreparado && vs.DuracionTotal > 0)
                {
                    // Calcula el porcentaje transcurrido (de 0.0 a 1.0)
                    float progreso = (float)(vs.TiempoActual / vs.DuracionTotal);
                    // Actualiza la posición visual del Slider
                    barraProgreso.SetValueWithoutNotify(progreso); 
                }
            }
        }
    }

    // Se ejecuta al arrastrar el slider
    private void AlMoverBarra(float valor)
    {
        if (appController.videoService != null)
        {
            appController.videoService.MoverTiempo(valor);
        }
    }

    // Mostrar/ocultar controller
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

    // Alternar el mostrar/ocultar dichos paneles
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

    // Acción del botón Pausar/Reanudar
    private void OnPlayPauseClicked()
    {
        appController.TogglePause();
        // Cambiar el texto de los botones pausar/Reanudar
        if (playPauseText != null)
            playPauseText.text = appController.State == AppState.Paused ? "Reanudar" : "Pausar";
    }

    // Acción del botón Volver al Menú
    private void OnBackClicked()
    {
        appController.BackToMenu();
        if (playPauseText != null) playPauseText.text = "Pausar";
        if (barraProgreso != null) barraProgreso.SetValueWithoutNotify(0);
        OcultarControles();
    }
}