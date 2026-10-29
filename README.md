# Reproductor de Video 360 VR - Meta Quest 3

Este proyecto es una aplicación de Realidad Virtual inmersiva desarrollada en **Unity** para **Meta Quest 3**. Permite la reproducción de videos en formato esférico ($360^\circ$) e incluye un sistema de menú con Realidad Mixta (Passthrough) y una interfaz de controles de reproducción interactiva espaciada en el entorno.

## Características Principales
- **Menú en Passthrough:** Entorno de Realidad Mixta transparente al seleccionar videos, integrando la interfaz con el mundo real.
- **Controles de Reproducción Inmersivos:** Consola flotante invocada físicamente con los controles de Quest (Botones A/B/X/Y) que incluye una barra de progreso dinámica (Slider interactivo con láser) y botones de Pausa/Reanudar.
- **Transiciones Suaves:** Sistema de doble fundido a negro (Fade-to-black) apoyado por pantallas de carga para evitar mareos al cambiar entre el entorno real y los videos.
- **Catálogo Dinámico:** Gestión eficiente de memoria mediante `ScriptableObjects` y carga de archivos locales pesados directamente desde el almacenamiento.

## Requisitos del Sistema
- **Unity:** 2022.3 LTS (o versión compatible con Meta XR Core SDK).
- **Librerías:** Meta XR Core SDK, Meta XR Interaction SDK OVR.
- **Git LFS:** Estrictamente requerido para clonar los videos de alta calidad.
- **Hardware:** Meta Quest 3 (Compatible con Meta Quest 2 y Pro).

## Cómo abrir el proyecto

1. Asegúrate de tener instalado [Git LFS](https://git-lfs.github.com/) en tu sistema.
2. Clona este repositorio en tu máquina local desde la terminal:
   ```bash
   git clone https://github.com/AlberthGD/Vr360Video.git
   ```
3. Navega a la carpeta del proyecto y fuerza la descarga de los archivos de video pesados:
   ```bash
   cd Vr360Video
   git lfs pull
   ```
4. Abre **Unity Hub**, haz clic en *Add project from disk* (Añadir proyecto desde el disco) y selecciona la carpeta clonada.
5. Una vez en Unity, abre la escena principal ubicada en `Assets/Scenes/360VideoAR.unity`.

## Dónde colocar los videos y configurar el catálogo

Para mantener el rendimiento óptimo de la aplicación y evitar que Unity colapse al cargar videos muy pesados en RAM, el proyecto los lee mediante rutas locales (StreamingAssets).

1. Coloca tus archivos de video en formato **`.mp4`** dentro de la carpeta: 
   `Assets/StreamingAssets/`
2. En Unity, ve a la carpeta `Assets/Videos/` y selecciona el archivo **Catalogo** (Es un `ScriptableObject` llamado `VideoCatalog`).
3. En la ventana del *Inspector*, despliega la lista `Videos` y añade un nuevo elemento:
   - **Title:** El nombre que aparecerá en el botón del menú.
   - **File Name:** El nombre exacto de tu video con extensión (ej. `MiVideo.mp4`).
   - **Thumbnail:** Arrastra una imagen 2D (Sprite) para que sirva de miniatura.
   - **Duration:** Texto descriptivo de la duración (ej. "03:45").
   - **Loop:** Marca la casilla si deseas que el video se repita infinitamente.

*Nota: Al usar Git, asegúrate de hacer commit a los nuevos videos, los cuales serán procesados automáticamente por Git LFS según el archivo `.gitattributes`.*

## Cómo compilar e instalar en Meta Quest (Build)

1. En el menú superior de Unity, ve a **File > Build Settings**.
2. Selecciona **Android** en la lista de plataformas. (Si no está activo, haz clic en el botón *Switch Platform* y espera a que recompile).
3. En la opción *Texture Compression*, asegúrate de que esté seleccionado **ASTC**.
4. Conecta tu visor Meta Quest al ordenador mediante un cable USB-C de transferencia de datos.
5. Asegúrate de que tu visor tenga el **Modo Desarrollador** activado (configurable desde la app de Meta Horizon en tu dispositivo móvil).
6. En la opción *Run Device*, selecciona tu visor Meta Quest.
7. Haz clic en **Build and Run**. Unity compilará el archivo `.apk` y lo instalará y ejecutará automáticamente en tu visor.