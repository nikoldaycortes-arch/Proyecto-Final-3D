
using UnityEngine;
using System.Collections;

public class AnimacionBoton : MonoBehaviour
{
    public float altura = 0.2f;
    public float duracion = 0.25f;

    private Vector3 posicionInicial;
    private bool animando = false;

    void Start()
    {
        posicionInicial = transform.localPosition;
    }

    public IEnumerator Brinquito()
    {
        if (animando) yield break;

        animando = true;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float movimiento = Mathf.Sin(
                (tiempo / duracion) * Mathf.PI
            ) * altura;

            transform.localPosition =
                posicionInicial + Vector3.up * movimiento;

            yield return null;
        }

        transform.localPosition = posicionInicial;
        animando = false;
    }
}

