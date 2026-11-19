using UnityEngine;
using UnityEngine.Video;

public class SuavizarLoopVideo : MonoBehaviour
{
    public float duracionFundido = 0.6f;
    private VideoPlayer reproductor;

    void Start()
    {
        reproductor = GetComponent<VideoPlayer>();
    }

    void Update()
    {
        if (reproductor == null || reproductor.length <= 0) return;

        double tiempoActual = reproductor.time;
        double duracionTotal = reproductor.length;

        if (tiempoActual < duracionFundido)
        {
            reproductor.targetCameraAlpha = (float)(tiempoActual / duracionFundido);
        }
        else if (tiempoActual > duracionTotal - duracionFundido)
        {
            reproductor.targetCameraAlpha = (float)((duracionTotal - tiempoActual) / duracionFundido);
        }
        else
        {
            reproductor.targetCameraAlpha = 1f;
        }
    }
}
