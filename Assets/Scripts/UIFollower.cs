using UnityEngine;

public class UIFollower : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;

    [Header("Ajustes de Seguimiento")]
    public float distanciaFrente = 2.0f;     // A cuántos metros del usuario estara
    public float velocidadMovimiento = 4.0f; // Que tan rapido persigue el panel al usuario
    public float velocidadRotacion = 6.0f;   // Que tan rápido gira 
    public float alturaOffset = -0.2f;   

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calcula hacia donde mira (Ignorando arriba y abajo para que no haya inclinamiento extraños)
        Vector3 forward = cameraTransform.forward;
        forward.y = 0; 
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        // Posicion frente al usuario
        Vector3 posicionObjetivo = cameraTransform.position + (forward * distanciaFrente);
        posicionObjetivo.y = cameraTransform.position.y + alturaOffset;

        // Mueve el canva suavemn et hacia esa posicion 
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, Time.deltaTime * velocidadMovimiento);

        // Rotacion del canvas 
        Vector3 direccionHaciaCamara = transform.position - cameraTransform.position;
        direccionHaciaCamara.y = 0; 
        
        if (direccionHaciaCamara.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHaciaCamara);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadRotacion);
        }
    }
}