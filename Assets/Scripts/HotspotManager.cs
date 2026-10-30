using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Puntos de interés (hotspots) dentro del video 360.
// Se definen en el catálogo (JSON o ScriptableObject) con coordenadas yaw/pitch y un rango de tiempo.
// El usuario apunta con el mando y presiona el gatillo para ver la información.
public class HotspotManager : MonoBehaviour
{
    [Header("Referencias")]
    public VideoService videoService;
    [Tooltip("Mientras la consola de reproducción está abierta, el gatillo es para sus botones y no para los hotspots")]
    public PlaybackControls playbackControls;
    [Tooltip("Origen del rayo de cada mando. Si se deja vacío se usan los anchors del OVRCameraRig")]
    public Transform rayoDerecho;
    public Transform rayoIzquierdo;
    [Tooltip("Corrección de inclinación del rayo (grados) si no coincide con el láser del mando")]
    public float correccionPitchRayo = 0f;

    [Header("Apariencia")]
    [Tooltip("Material base (Sprites/Default). Garantiza que el shader se incluya en el build")]
    public Material materialBase;
    public float radio = 6f;               // Distancia de los hotspots a la cámara
    public float tamano = 0.35f;            // Diámetro del marcador
    public Color colorNormal = new(0.2f, 0.75f, 1f, 0.9f);
    public Color colorHover = new(1f, 0.85f, 0.2f, 1f);
    public float distanciaPanel = 2.2f;     // Distancia del panel de información
    public float autoOcultarPanel = 10f;     // Segundos (0 = no se oculta solo)

    [Header("Herramienta de autor")]
    [Tooltip("Al presionar el joystick derecho muestra el yaw/pitch/tiempo hacia donde apunta el rayo (para llenar el JSON)")]
    public bool mostrarCoordenadas = true;

    private class Marcador
    {
        public HotspotData data;
        public GameObject go;
        public Renderer rend;
        public Collider col;
        public Vector3 direccion;
    }

    private readonly List<Marcador> marcadores = new();
    private VideoEntry videoMostrado;
    private Material matMarcador, matPanel, matLinea;
    private GameObject panel;
    private TextMeshPro panelTitulo, panelTexto;
    private Vector3 panelDireccion;
    private float panelTiempo;
    private LineRenderer linea;
    private Marcador hover;
    private float rotacionSkybox;

    void Awake()
    {
        if (materialBase != null) matMarcador = new Material(materialBase);
        else
        {
            Shader shSprite = Shader.Find("Sprites/Default");
            matMarcador = new Material(shSprite != null ? shSprite : Shader.Find("Universal Render Pipeline/Unlit"));
        }
        matPanel = new Material(matMarcador) { color = new Color(0.05f, 0.07f, 0.12f, 0.85f) };
        matLinea = new Material(matMarcador);

        CrearPanel();
        CrearLinea();
    }

    void Start()
    {
        if (rayoDerecho == null || rayoIzquierdo == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                if (rayoDerecho == null) rayoDerecho = rig.rightControllerAnchor;
                if (rayoIzquierdo == null) rayoIzquierdo = rig.leftControllerAnchor;
            }
        }
    }

    // ---------- API usada por AppController ----------

    public void Mostrar(VideoEntry entry)
    {
        if (entry == videoMostrado) return; // Ya están creados (p. ej. al reanudar desde pausa)
        Limpiar();
        videoMostrado = entry;
        if (entry == null) return;

        // Si el skybox tiene rotación, los hotspots giran con él
        var sky = RenderSettings.skybox;
        rotacionSkybox = (sky != null && sky.HasProperty("_Rotation")) ? sky.GetFloat("_Rotation") : 0f;

        if (entry.hotspots == null) return;
        foreach (var h in entry.hotspots) CrearMarcador(h);
    }

    public void Limpiar()
    {
        foreach (var m in marcadores) if (m.go != null) Destroy(m.go);
        marcadores.Clear();
        videoMostrado = null;
        hover = null;
        OcultarPanel();
        if (linea != null) linea.enabled = false;
    }

    // ---------- Conversión de coordenadas ----------

    // El shader Skybox/Panoramic coloca el centro del video en +X (yaw de Unity = 90° + _Rotation)
    public Vector3 DireccionDesdeYawPitch(float yaw, float pitch)
    {
        return Quaternion.Euler(-pitch, 90f + rotacionSkybox + yaw, 0f) * Vector3.forward;
    }

    public Vector2 YawPitchDesdeDireccion(Vector3 d)
    {
        d.Normalize();
        float yawMundo = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        float yaw = Mathf.DeltaAngle(90f + rotacionSkybox, yawMundo);
        float pitch = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
        return new Vector2(yaw, pitch);
    }

    // ---------- Ciclo ----------

    void LateUpdate()
    {
        if (videoMostrado == null || Camera.main == null) return;

        Transform cam = Camera.main.transform;
        double t = videoService != null ? videoService.TiempoActual : 0;

        // Posición: el video 360 está "en el infinito", así que los hotspots siguen la posición de la cabeza
        foreach (var m in marcadores)
        {
            bool visible = t >= m.data.startTime && (m.data.endTime < 0 || t <= m.data.endTime);
            if (m.go.activeSelf != visible) m.go.SetActive(visible);
            if (!visible) continue;

            m.go.transform.position = cam.position + m.direccion * radio;
            m.go.transform.rotation = Quaternion.LookRotation(m.go.transform.position - cam.position);

            float pulso = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
            float escala = (m == hover ? 1.35f : 1f) * pulso * tamano;
            m.go.transform.localScale = Vector3.one * escala;
            m.rend.material.color = m == hover ? colorHover : colorNormal;
        }

        if (panel.activeSelf)
        {
            panel.transform.position = cam.position + panelDireccion * distanciaPanel;
            panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - cam.position);
            if (autoOcultarPanel > 0 && Time.time - panelTiempo > autoOcultarPanel) OcultarPanel();
        }

        Physics.SyncTransforms(); // Los colliders usan la posición de este frame
        ProcesarEntrada();
    }

    private void ProcesarEntrada()
    {
        hover = null;
        bool clic = false;
        Ray rayo = default;
        bool hayRayo = false;

        // Mando derecho (prioridad) o izquierdo
        if (rayoDerecho != null && OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
        {
            rayo = CrearRayo(rayoDerecho);
            hayRayo = true;
            clic = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        }
        else if (rayoIzquierdo != null && OVRInput.IsControllerConnected(OVRInput.Controller.LTouch))
        {
            rayo = CrearRayo(rayoIzquierdo);
            hayRayo = true;
            clic = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        }

        // En el Editor: mouse
        if (!hayRayo && Mouse.current != null && Camera.main != null)
        {
            rayo = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            hayRayo = true;
            clic = Mouse.current.leftButton.wasPressedThisFrame;
        }
        if (!hayRayo) return;

        bool consolaAbierta = playbackControls != null && playbackControls.panelVisual != null && playbackControls.panelVisual.activeSelf;
        hover = consolaAbierta ? null : Raycast(rayo);
        DibujarLinea(rayo, hover);

        if (clic)
        {
            if (hover != null) AbrirPanel(hover);
            else if (panel.activeSelf && !consolaAbierta && !ApuntaAUI(rayo)) OcultarPanel();
        }

        // Herramienta para obtener coordenadas al crear el JSON
        bool pedirCoord = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) ||
                          (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);
        if (mostrarCoordenadas && pedirCoord)
        {
            Vector2 yp = YawPitchDesdeDireccion(rayo.direction);
            double t = videoService != null ? videoService.TiempoActual : 0;
            string msg = $"\"yaw\": {yp.x:F1}, \"pitch\": {yp.y:F1}, \"startTime\": {t:F1}";
            Debug.Log("[Hotspot] " + msg);
            MostrarPanel("Coordenadas", msg.Replace(", ", "\n"), rayo.direction);
        }
    }

    private Ray CrearRayo(Transform origen)
    {
        Vector3 dir = Quaternion.AngleAxis(correccionPitchRayo, origen.right) * origen.forward;
        return new Ray(origen.position, dir);
    }

    // Devuelve el marcador más cercano, ignorándolo si hay UI (otro collider) delante
    private Marcador Raycast(Ray rayo)
    {
        var hits = Physics.RaycastAll(rayo, radio * 2f, ~0, QueryTriggerInteraction.Collide);
        float distUI = float.MaxValue;
        Marcador mejor = null;
        float mejorDist = float.MaxValue;

        foreach (var h in hits)
        {
            var m = marcadores.Find(x => x.col == h.collider);
            if (m != null) { if (h.distance < mejorDist) { mejorDist = h.distance; mejor = m; } }
            else distUI = Mathf.Min(distUI, h.distance);
        }
        return (mejor != null && mejorDist < distUI) ? mejor : null;
    }

    private bool ApuntaAUI(Ray rayo)
    {
        foreach (var h in Physics.RaycastAll(rayo, radio * 2f))
            if (marcadores.Find(x => x.col == h.collider) == null) return true;
        return false;
    }

    // ---------- Construcción de objetos ----------

    private void CrearMarcador(HotspotData h)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Hotspot_" + h.title;
        go.transform.SetParent(transform, false);
        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = matMarcador;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.75f; // Área de selección más grande que el marcador visible

        // Etiqueta con el título debajo del marcador
        var label = new GameObject("Etiqueta").AddComponent<TextMeshPro>();
        label.transform.SetParent(go.transform, false);
        label.transform.localPosition = new Vector3(0, -0.9f, 0);
        label.transform.localRotation = Quaternion.identity;
        label.text = h.title;
        label.fontSize = 6f;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(8f, 1f);

        marcadores.Add(new Marcador
        {
            data = h, go = go, rend = rend, col = col,
            direccion = DireccionDesdeYawPitch(h.yaw, h.pitch)
        });
    }

    private void CrearPanel()
    {
        panel = new GameObject("PanelHotspot");
        panel.transform.SetParent(transform, false);

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(panel.transform, false);
        fondo.transform.localPosition = new Vector3(0, 0, 0.01f);
        fondo.transform.localScale = new Vector3(1.1f, 0.62f, 1f);
        fondo.GetComponent<Renderer>().sharedMaterial = matPanel;

        panelTitulo = new GameObject("Titulo").AddComponent<TextMeshPro>();
        panelTitulo.transform.SetParent(panel.transform, false);
        panelTitulo.rectTransform.sizeDelta = new Vector2(1f, 0.12f);
        panelTitulo.transform.localPosition = new Vector3(0, 0.22f, 0);
        panelTitulo.fontSize = 0.7f;
        panelTitulo.fontStyle = FontStyles.Bold;
        panelTitulo.color = colorHover;
        panelTitulo.alignment = TextAlignmentOptions.Center;

        panelTexto = new GameObject("Descripcion").AddComponent<TextMeshPro>();
        panelTexto.transform.SetParent(panel.transform, false);
        panelTexto.rectTransform.sizeDelta = new Vector2(1f, 0.4f);
        panelTexto.transform.localPosition = new Vector3(0, -0.06f, 0);
        panelTexto.fontSize = 0.45f;
        panelTexto.alignment = TextAlignmentOptions.Top;
        panelTexto.textWrappingMode = TextWrappingModes.Normal;

        panel.SetActive(false);
    }

    private void CrearLinea()
    {
        linea = new GameObject("RayoHotspot").AddComponent<LineRenderer>();
        linea.transform.SetParent(transform, false);
        linea.sharedMaterial = matLinea;
        linea.positionCount = 2;
        linea.widthMultiplier = 0.004f;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.enabled = false;
    }

    private void DibujarLinea(Ray rayo, Marcador objetivo)
    {
        if (linea == null) return;
        // Solo se dibuja cuando apunta a un hotspot (el láser del SDK sigue funcionando para la UI)
        linea.enabled = objetivo != null;
        if (!linea.enabled) return;
        linea.startColor = linea.endColor = colorHover;
        linea.SetPosition(0, rayo.origin);
        linea.SetPosition(1, objetivo.go.transform.position);
    }

    private void AbrirPanel(Marcador m)
    {
        MostrarPanel(m.data.title, m.data.description, m.direccion);
    }

    private void MostrarPanel(string titulo, string texto, Vector3 direccion)
    {
        panelTitulo.text = titulo;
        panelTexto.text = texto;
        // El panel aparece un poco debajo del hotspot para no taparlo
        panelDireccion = (direccion.normalized + Vector3.down * 0.25f).normalized;
        panelTiempo = Time.time;
        panel.SetActive(true);
    }

    private void OcultarPanel()
    {
        if (panel != null) panel.SetActive(false);
    }
}
