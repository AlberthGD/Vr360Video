using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float intervaloActualizacion = 0.5f;

    private float tiempoAcumulado = 0f;
    private int fotogramas = 0;
    private float tiempoRestante;

    void Start()
    {
        tiempoRestante = intervaloActualizacion;
    }

    void Update()
    {
        tiempoRestante -= Time.unscaledDeltaTime;
        tiempoAcumulado += Time.unscaledDeltaTime;
        fotogramas++;

        if (tiempoRestante <= 0f)
        {
            float fps = fotogramas / tiempoAcumulado;
            if (fpsText != null)
            {
                fpsText.text = $"{Mathf.RoundToInt(fps)} FPS";
                fpsText.color = fps >= 70 ? Color.green : (fps >= 50 ? Color.yellow : Color.red);    // Verde si mantiene los 72/90/120 Hz, amarillo o rojo si cae
            }

            tiempoRestante = intervaloActualizacion;
            tiempoAcumulado = 0f;
            fotogramas = 0;
        }
    }
}