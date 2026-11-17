using UnityEngine;
using System.Collections;

public class AnimacionBoton : MonoBehaviour
{
    public float presion = 0.08f;
    public float duracion = 0.10f;

    private Vector3 escalaInicial;
    private bool animando = false;

    void Start()
    {
        escalaInicial = transform.localScale;
    }

    public IEnumerator Brinquito()
    {
        if (animando) yield break;

        animando = true;

        Vector3 escalaPresionada = new Vector3(
            escalaInicial.x * 0.98f,
            escalaInicial.y * (1f - presion),
            escalaInicial.z
        );

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            transform.localScale = Vector3.Lerp(
                escalaInicial,
                escalaPresionada,
                tiempo / duracion
            );

            yield return null;
        }

        tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            transform.localScale = Vector3.Lerp(
                escalaPresionada,
                escalaInicial,
                tiempo / duracion
            );

            yield return null;
        }

        transform.localScale = escalaInicial;
        animando = false;
    }
}
