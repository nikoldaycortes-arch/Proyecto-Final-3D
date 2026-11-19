using UnityEngine;
using System.Collections;

/// <summary>
/// Enemigo que persigue al jugador.
///  - Si el jugador lo pisa desde arriba -> se aplasta (lo hace MovoJugadorTPV).
///  - Si el jugador lo golpea con el giro -> muere (lo hace MovoJugadorTPV).
///  - Si lo toca por el lado -> el jugador muere.
/// </summary>
public class Enemigo : MonoBehaviour
{
    public Transform player;
    public float velocidadPersecucion = 3f;
    public float distanciaDeteccion = 10f;
    public float gravedad = -25f;

    [Header("Reaparición (opcional)")]
    public bool reaparecer = false;
    public float tiempoReaparicion = 3f;

    [HideInInspector] public bool estaVivo = true;

    private CharacterController controller;
    private MovoJugadorTPV jugador;
    private float velocidadY;
    private Vector3 escalaOriginal;
    private Vector3 posicionInicial;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        if (player != null) jugador = player.GetComponent<MovoJugadorTPV>();

        escalaOriginal = transform.localScale;
        posicionInicial = transform.position;

        // NOTA: se eliminó Physics.IgnoreCollision. Ignorar la colisión con el jugador
        // impedía que se detectara el pisotón y el contacto lateral.
    }

    void Update()
    {
        if (!estaVivo || player == null) return;

        Vector3 movimiento = Vector3.zero;
        float distancia = Vector3.Distance(transform.position, player.position);

        if (distancia < distanciaDeteccion)
        {
            Vector3 direccion = player.position - transform.position;
            direccion.y = 0f;
            direccion.Normalize();

            movimiento = direccion * velocidadPersecucion;

            if (direccion != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direccion);
        }

        // Gravedad para que no flote
        if (controller.isGrounded && velocidadY < 0f) velocidadY = -2f;
        velocidadY += gravedad * Time.deltaTime;
        movimiento.y = velocidadY;

        controller.Move(movimiento * Time.deltaTime);
    }

    // El enemigo toca al jugador por el lado -> el jugador muere
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!estaVivo) return;

        MovoJugadorTPV j = hit.gameObject.GetComponent<MovoJugadorTPV>();
        if (j == null || !j.EstaVivo) return;

        // Si el jugador está por encima de la cabeza del enemigo, no lo daña
        // (el pisotón lo resuelve el script del jugador)
        float piesJugador = hit.collider.bounds.min.y;
        float cabezaEnemigo = controller.bounds.max.y;
        if (piesJugador >= cabezaEnemigo - 0.2f) return;

        j.Morir();
    }

    // Pisotón: se aplasta
    public void Aplastar()
    {
        if (!estaVivo) return;
        StartCoroutine(AplastarseYRetirarse());
    }

    // Giro: sale despedido/desaparece
    public void Morir()
    {
        if (!estaVivo) return;
        StartCoroutine(Desvanecerse());
    }

    private IEnumerator AplastarseYRetirarse()
    {
        estaVivo = false;
        controller.enabled = false;

        transform.localScale = new Vector3(escalaOriginal.x * 1.8f, 0.15f, escalaOriginal.z * 1.8f);
        yield return new WaitForSeconds(0.5f);

        FinalizarMuerte();
    }

    private IEnumerator Desvanecerse()
    {
        estaVivo = false;
        controller.enabled = false;

        float t = 0f;
        const float duracion = 0.3f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, Vector3.zero, t / duracion);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f);
            yield return null;
        }

        FinalizarMuerte();
    }

    private void FinalizarMuerte()
    {
        if (!reaparecer)
        {
            Destroy(gameObject);
            return;
        }
        StartCoroutine(Renacer());
    }

    private IEnumerator Renacer()
    {
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(tiempoReaparicion);

        transform.position = posicionInicial;
        transform.localScale = escalaOriginal;
        velocidadY = 0f;
        controller.enabled = true;
        estaVivo = true;
    }
}
