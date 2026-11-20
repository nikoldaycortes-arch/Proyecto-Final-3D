using UnityEngine;

public class ButtonsMOV : MonoBehaviour
{
    public float amplitud = 0.02f;
    public float velocidad = 2f;
    public float desfaseTiempo = 0f;
    private Vector3 escalaInicial;

    void Start()
    {
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        float factor = 1f + Mathf.Sin((Time.time + desfaseTiempo) * velocidad) * amplitud;
        transform.localScale = escalaInicial * factor;
    }
}