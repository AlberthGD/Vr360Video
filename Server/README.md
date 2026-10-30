# Catálogo remoto, video estéreo y hotspots

## 1. Configuración en la escena (una sola vez)
1. En `AppManager` agrega los componentes **RemoteCatalogLoader** y **HotspotManager**.
2. **RemoteCatalogLoader**: arrastra `MenuView` y el ScriptableObject `Assets/Videos/Catalogo` (respaldo y miniaturas locales). `Status Text` es opcional.
3. **MenuView**: arrastra el `RemoteCatalogLoader` al campo `Catalog Loader`.
4. **HotspotManager**: arrastra el `VideoService`. Los rayos se toman solos del `OVRCameraRig`.
5. **AppController**: arrastra el `HotspotManager` al campo `Hotspot Manager`.

## 2. Catálogo JSON desde un servidor (agregar videos sin recompilar)
La app descarga `catalogUrl` al iniciar y cada vez que se vuelve al menú.
Si no hay red usa: caché de la última descarga → `StreamingAssets/catalog.json` → ScriptableObject.

**Opción A – GitHub (HTTPS, recomendada):** haz push de esta carpeta `Server/` y deja la URL por defecto:
`https://raw.githubusercontent.com/AlberthGD/Vr360Video/main/Server/catalog.json`
Para agregar un video: edita `catalog.json`, haz commit/push y vuelve al menú en el visor.

**Opción B – Servidor local en la PC (misma red Wi-Fi que el Quest):**
```bash
cd Server
python -m http.server 8000
```
URL: `http://IP_DE_TU_PC:8000/catalog.json`. Para HTTP en Android activa
*Player Settings > Other Settings > Allow downloads over HTTP = Always allowed*.

### Campos de cada video
| Campo | Descripción |
|---|---|
| `title`, `duration`, `loop` | Igual que en el ScriptableObject |
| `fileName` | Video dentro de `StreamingAssets` (ya instalado en la app) |
| `url` | (Opcional) URL remota del .mp4; si existe se reproduce por streaming. **No** sirve un archivo de Git LFS desde raw.githubusercontent; usa el servidor local u otro hosting |
| `thumbnailUrl` | (Opcional) URL de la miniatura. Si falla, usa el Sprite local con el mismo `fileName` |
| `stereo` | `"Mono"`, `"OverUnder"` o `"SideBySide"` |
| `hotspots` | Lista de puntos de interés (ver abajo) |

## 3. Video estéreo Over Under
Con `"stereo": "OverUnder"` el `AppController` asigna `_Layout = 2` al material `Skybox/Panoramic`
(mitad superior = ojo izquierdo, inferior = ojo derecho). Se usa una copia del material para no modificar el `.mat`.
Recomendado: subir la `RenderTexture` Textura360 a 4096×4096 si se usan videos OU de alta resolución.

## 4. Hotspots
```json
{ "title": "Edificio principal", "description": "Texto informativo",
  "yaw": 0, "pitch": 5, "startTime": 0, "endTime": -1 }
```
- `yaw`: grados horizontales desde el centro del video (+ derecha, − izquierda). `pitch`: + arriba, − abajo.
- `startTime`/`endTime`: segundos en que es visible (`-1` = hasta el final).
- En el visor: apunta con el mando y presiona el **gatillo** para abrir la información; gatillo en el vacío la cierra.
- **Obtener coordenadas:** durante el video apunta a un objeto y presiona el **joystick derecho** (o la tecla **C** en el Editor). Se muestran y se escriben en consola el `yaw`, `pitch` y `startTime` listos para pegar en el JSON.
- Las coordenadas de `catalog.json` son de ejemplo: ajústalas con esa herramienta.
