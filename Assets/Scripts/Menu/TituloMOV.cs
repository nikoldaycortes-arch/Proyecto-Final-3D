using UnityEngine;

public class TituloMOV : MonoBehaviour
{
    public float amplitud = 0.06f;
    public float velocidad = 2.5f;
    private Vector3 escalaInicial;

    void Start()
    {
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        float factor = 1f + Mathf.Sin(Time.time * velocidad) * amplitud;
        transform.localScale = escalaInicial * factor;
    }
}