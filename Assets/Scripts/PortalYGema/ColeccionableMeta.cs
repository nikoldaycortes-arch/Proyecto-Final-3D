using UnityEngine;
using UnityEngine.Events;

public class ColeccionableMeta : MonoBehaviour
{
    public float velocidadGiro = 90f;
    public float amplitudFlotacion = 0.15f;
    public float frecuenciaFlotacion = 2f;
    public UnityEvent alRecoger;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
        float desplazamientoY = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = posicionInicial + Vector3.up * desplazamientoY;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            alRecoger?.Invoke();
            Destroy(gameObject);
        }
    }
}