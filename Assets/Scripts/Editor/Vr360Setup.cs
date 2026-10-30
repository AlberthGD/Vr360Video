using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Configura la escena para catálogo remoto, video estéreo y hotspots, y valida todo.
// Se ejecuta sola una vez al compilar y también desde el menú Vr360 > Configurar y validar escena.
[InitializeOnLoad]
public static class Vr360Setup
{
    const string ScenePath = "Assets/Scenes/360VideoAR.unity";
    const string CatalogPath = "Assets/Videos/Catalogo.asset";
    const string MatPath = "Assets/Resources/HotspotMat.mat";
    const string ReportPath = "Logs/Vr360Validacion.txt";
    static string FlagKey => "Vr360Setup_v1_" + Application.dataPath;

    static Vr360Setup()
    {
        if (EditorPrefs.GetBool(FlagKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            EditorPrefs.SetBool(FlagKey, true);
            ConfigurarYValidar();
        };
    }

    [MenuItem("Vr360/Configurar y validar escena")]
    public static void ConfigurarYValidar()
    {
        var r = new StringBuilder();
        r.AppendLine($"=== Validación Vr360 ({System.DateTime.Now}) ===");
        int errores = 0;
        void Ok(string m) => r.AppendLine("[OK]    " + m);
        void Err(string m) { r.AppendLine("[ERROR] " + m); errores++; }
        void Info(string m) => r.AppendLine("[INFO]  " + m);

        // --- Escena ---
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.path != ScenePath)
        {
            if (escena.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            { Debug.LogWarning("[Vr360] Cancelado: escena sin guardar."); return; }
            escena = EditorSceneManager.OpenScene(ScenePath);
        }

        var app = Object.FindFirstObjectByType<AppController>(FindObjectsInactive.Include);
        var menu = Object.FindFirstObjectByType<MenuView>(FindObjectsInactive.Include);
        var vs = Object.FindFirstObjectByType<VideoService>(FindObjectsInactive.Include);
        var catalogo = AssetDatabase.LoadAssetAtPath<VideoCatalog>(CatalogPath);

        if (app == null || menu == null || vs == null) { Err("No se encontró AppController / MenuView / VideoService en la escena"); Guardar(r, errores); return; }
        Ok($"AppController en '{app.name}', MenuView en '{menu.name}', VideoService en '{vs.name}'");

        // Material para los hotspots (asegura que el shader entre al build)
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default")) { name = "HotspotMat" };
            AssetDatabase.CreateAsset(mat, MatPath);
            Ok("Creado " + MatPath);
        }

        // --- Componentes y referencias ---
        var go = app.gameObject;
        // (En el Editor GetComponent devuelve un "null falso", por eso no se usa ??)
        var loader = go.GetComponent<RemoteCatalogLoader>();
        if (loader == null) loader = Undo.AddComponent<RemoteCatalogLoader>(go);
        var hot = go.GetComponent<HotspotManager>();
        if (hot == null) hot = Undo.AddComponent<HotspotManager>(go);

        Undo.RecordObjects(new Object[] { loader, hot, menu, app }, "Vr360 Setup");
        loader.menuView = menu;
        if (loader.catalogoLocal == null) loader.catalogoLocal = catalogo != null ? catalogo : menu.catalog;
        menu.catalogLoader = loader;
        hot.videoService = vs;
        if (hot.playbackControls == null) hot.playbackControls = app.playbackControls;
        if (hot.materialBase == null) hot.materialBase = mat;
        app.hotspotManager = hot;
        foreach (var o in new Object[] { loader, hot, menu, app }) EditorUtility.SetDirty(o);

        Ok("RemoteCatalogLoader y HotspotManager agregados y conectados en " + go.name);
        if (app.videoSkybox == null) Err("AppController.videoSkybox está vacío");
        else if (!app.videoSkybox.HasProperty("_Layout")) Err($"El material {app.videoSkybox.name} no tiene _Layout (debe usar Skybox/Panoramic)");
        else Ok($"Material {app.videoSkybox.name} ({app.videoSkybox.shader.name}) soporta _Layout (estéreo)");

        if (Object.FindFirstObjectByType<OVRCameraRig>(FindObjectsInactive.Include) != null) Ok("OVRCameraRig encontrado (origen de los rayos de hotspots)");
        else Err("No hay OVRCameraRig: asigna Rayo Derecho/Izquierdo manualmente en HotspotManager");

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
        Ok("Escena guardada");

        // --- Catálogo JSON ---
        foreach (var ruta in new[] { "Assets/StreamingAssets/catalog.json", "Server/catalog.json" })
        {
            if (!File.Exists(ruta)) { Err(ruta + " no existe"); continue; }
            try
            {
                var data = JsonUtility.FromJson<CatalogData>(File.ReadAllText(ruta));
                Ok($"{ruta}: {data.videos.Count} videos, versión {data.version}");
                foreach (var v in data.videos)
                {
                    bool existe = !string.IsNullOrEmpty(v.url) || File.Exists(Path.Combine("Assets/StreamingAssets", v.fileName ?? ""));
                    string linea = $"   - {v.title}: stereo='{v.stereo}' -> _Layout={v.StereoLayout}, hotspots={v.hotspots?.Count ?? 0}, video {(existe ? "OK" : "NO ENCONTRADO")}";
                    if (existe) r.AppendLine(linea); else Err(linea);
                }
            }
            catch (System.Exception e) { Err($"{ruta}: JSON inválido ({e.Message})"); }
        }

        // --- Estéreo ---
        var prueba = new VideoEntry();
        string[] modos = { "Mono", "OverUnder", "over_under", "OU", "SideBySide", "SBS", "" };
        int[] esperado = { 0, 2, 2, 2, 1, 1, 0 };
        bool estereoOk = true;
        for (int i = 0; i < modos.Length; i++) { prueba.stereo = modos[i]; if (prueba.StereoLayout != esperado[i]) { estereoOk = false; Err($"stereo '{modos[i]}' -> {prueba.StereoLayout}, esperado {esperado[i]}"); } }
        if (estereoOk) Ok("Conversión stereo -> _Layout correcta (Mono=0, SideBySide=1, OverUnder=2)");

        // --- Hotspots: conversión yaw/pitch ida y vuelta ---
        float maxErr = 0;
        foreach (var (y, p) in new[] { (0f, 0f), (90f, -10f), (-60f, 45f), (179f, -20f), (30f, 15f) })
        {
            var d = hot.DireccionDesdeYawPitch(y, p);
            var yp = hot.YawPitchDesdeDireccion(d);
            maxErr = Mathf.Max(maxErr, Mathf.Abs(Mathf.DeltaAngle(yp.x, y)), Mathf.Abs(yp.y - p));
        }
        var centro = hot.DireccionDesdeYawPitch(0, 0);
        if (maxErr < 0.01f) Ok($"Conversión yaw/pitch <-> dirección correcta (error máx {maxErr:F4}°). Centro del video = {centro}");
        else Err($"Conversión yaw/pitch con error {maxErr}°");

        // --- Ajustes de Android ---
        Info($"Allow downloads over HTTP = {PlayerSettings.insecureHttpOption} (solo necesario para servidor local http://)");
        var plataforma = EditorUserBuildSettings.activeBuildTarget;
        Info("Plataforma activa: " + plataforma);

        Guardar(r, errores);
    }

    static void Guardar(StringBuilder r, int errores)
    {
        r.AppendLine(errores == 0 ? "RESULTADO: todo correcto" : $"RESULTADO: {errores} error(es)");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, r.ToString());
        if (errores == 0) Debug.Log("[Vr360] " + r); else Debug.LogError("[Vr360] " + r);
    }
}
