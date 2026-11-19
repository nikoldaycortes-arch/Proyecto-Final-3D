using UnityEngine;

public class CamaraTerceraPersona : MonoBehaviour
{
    public Transform target;
    [Range(0f, 80f)]
    public float inclinacion = 25f;
    public float distancia = 6f;
    public float suavizado = 8f;
    public float alturaMira = 1.3f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 puntoMira = target.position + Vector3.up * alturaMira;
        Quaternion rotacion = Quaternion.Euler(inclinacion, target.eulerAngles.y, 0f);
        Vector3 posicionDeseada = puntoMira - (rotacion * Vector3.forward * distancia);

        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);
        transform.LookAt(puntoMira);
    }
}