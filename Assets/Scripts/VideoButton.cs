using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class VideoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI titleText; // Titulo del video
    public TextMeshProUGUI durationText; // Duracion del video
    public Image thumbnailImage; // Imagen de miniatura
    public Button buttonComponent; // El componente boton de este mismo objeto

    private VideoEntry myVideo;
    private AppController controller;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Configurar(VideoEntry video, AppController appCtrl)
    {
        myVideo = video;
        controller = appCtrl;
        titleText.text = video.title;
        durationText.text = video.duration;
        if (video.thumbnail != null) thumbnailImage.sprite = video.thumbnail;
        buttonComponent.onClick.AddListener(() => controller.SelectVideo(myVideo));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.1f; 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}