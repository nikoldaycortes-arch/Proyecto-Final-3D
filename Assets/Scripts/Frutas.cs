using UnityEngine;
using TMPro;

public class Fruta : MonoBehaviour
{
    public TMP_Text textoContador;
    public float velocidadGiro = 120f;
    public int valor = 1;

    private static int totalFrutas = 0;

    void Start()
    {
        totalFrutas = 0;
        if (textoContador != null)
        {
            textoContador.text = totalFrutas.ToString();
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            totalFrutas += valor;
            if (textoContador != null)
            {
                textoContador.text = totalFrutas.ToString();
            }
            Destroy(gameObject);
        }
    }
}