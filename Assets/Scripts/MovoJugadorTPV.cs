using System.Collections;
using UnityEngine;

/// <summary>
/// Jugador 3D de plataformas: correr, salto + doble salto, giro de ataque,
/// pisotón sobre enemigos y muerte con reaparición.
///
/// Parámetros del Animator (nómbralos exactamente así):
///   Float   Velocidad
///   Bool    EnPiso
///   Trigger Saltar   (se usa también para el doble salto)
///   Trigger Girar
///   Trigger Morir
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovoJugadorTPV : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movimiento")]
    public float veloMovi = 6f;
    public float veloRota = 12f;
    public float X, Z;

    [Header("Referencias")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private Animator animator;

    [Header("Piso")]
    public Transform checkPiso;
    public float distanciaPiso = 0.4f;
    public LayerMask piso;
    public float gravedad = -25f;

    [Header("Salto")]
    public float salto = 1.6f;                 // altura del primer salto
    public int saltosMaximos = 2;              // 2 = doble salto
    [Range(0.5f, 1.2f)]
    public float factorDobleSalto = 0.9f;      // el 2.º salto sube un poco menos
    public float tiempoCoyote = 0.1f;          // margen para saltar justo al salir del borde
    public float alturaRebote = 1.0f;          // rebote al pisar un enemigo

    [Header("Giro de ataque")]
    public float duracionGiro = 0.5f;
    public float cooldownGiro = 0.2f;
    public float radioGiro = 1.6f;
    public float alturaCentroGiro = 1f;

    [Header("Muerte")]
    public float tiempoReaparicion = 2f;
    public Vector3 puntoReaparicion;           // se llena solo en Start

    private Vector3 veloJugador;
    private bool enPiso;
    private int saltosHechos;
    private float ultimoTiempoPiso;
    private bool girando;
    private float proximoGiro;
    private bool vivo = true;

    public bool EstaVivo => vivo;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (followCamera == null) followCamera = Camera.main;
        puntoReaparicion = transform.position;
    }

    private void Update()
    {
        if (vivo)
        {
            Movimiento();
            Entradas();
        }
        AplicarGravedad();
        ActualizarAnimator();
    }

    // ---------------- MOVIMIENTO ----------------
    private void Movimiento()
    {
        // CAMBIO: ahora también usa isGrounded del CharacterController,
        // así funciona aunque la Layer o el CheckPiso estén mal configurados.
        enPiso = controller.isGrounded ||
                 (checkPiso != null && Physics.CheckSphere(checkPiso.position, distanciaPiso, piso));

        // Solo cuenta como "aterrizado" si ya no está subiendo.
        // (Evita que el doble salto se reinicie en el primer frame del salto.)
        if (enPiso && veloJugador.y <= 0f)
        {
            veloJugador.y = -2f;
            saltosHechos = 0;
            ultimoTiempoPiso = Time.time;
        }

        X = Input.GetAxisRaw("Horizontal");
        Z = Input.GetAxisRaw("Vertical");

        // La dirección es relativa al giro (yaw) de la cámara satélite
        Vector3 moveInput = Quaternion.Euler(0, followCamera.transform.eulerAngles.y, 0) * new Vector3(X, 0, Z);
        Vector3 moveDirection = moveInput.normalized;

        controller.Move(moveDirection * veloMovi * Time.deltaTime);

        if (moveDirection != Vector3.zero)
        {
            Quaternion rotacion = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, veloRota * Time.deltaTime);
        }
    }

    private void Entradas()
    {
        if (Input.GetButtonDown("Jump")) IntentarSalto();

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(0))
            && !girando && Time.time >= proximoGiro)
        {
            StartCoroutine(Girar());
        }
    }

    private void AplicarGravedad()
    {
        veloJugador.y += gravedad * Time.deltaTime;
        controller.Move(veloJugador * Time.deltaTime);
    }

    // ---------------- SALTO / DOBLE SALTO ----------------
    private void IntentarSalto()
    {
        bool dentroDeCoyote = Time.time - ultimoTiempoPiso <= tiempoCoyote;

        if (enPiso || (dentroDeCoyote && saltosHechos == 0))
        {
            EjecutarSalto(1f);
            saltosHechos = 1;
        }
        else if (saltosHechos < saltosMaximos)
        {
            // Si cayó de una plataforma sin saltar, igual solo tiene 1 salto en el aire
            if (saltosHechos == 0) saltosHechos = 1;
            EjecutarSalto(factorDobleSalto);
            saltosHechos++;
        }
    }

    private void EjecutarSalto(float factor)
    {
        veloJugador.y = Mathf.Sqrt(salto * -2f * gravedad) * factor;
        if (animator != null) animator.SetTrigger("Saltar");
    }

    // ---------------- GIRO DE ATAQUE ----------------
    private IEnumerator Girar()
    {
        girando = true;
        if (animator != null) animator.SetTrigger("Girar");

        float t = 0f;
        while (t < duracionGiro)
        {
            t += Time.deltaTime;
            GolpearEnemigos();
            yield return null;
        }

        girando = false;
        proximoGiro = Time.time + cooldownGiro;
    }

    private void GolpearEnemigos()
    {
        Vector3 centro = transform.position + Vector3.up * alturaCentroGiro;
        Collider[] cercanos = Physics.OverlapSphere(centro, radioGiro);
        foreach (Collider c in cercanos)
        {
            Enemigo enemigo = c.GetComponentInParent<Enemigo>();
            if (enemigo != null && enemigo.estaVivo) enemigo.Morir();
        }
    }

    // ---------------- PISOTÓN SOBRE ENEMIGOS ----------------
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Enemigo enemigo = hit.collider.GetComponentInParent<Enemigo>();
        if (enemigo == null || !enemigo.estaVivo) return;

        // Cayendo y golpeando la parte de arriba del enemigo
        if (veloJugador.y <= 0f && hit.normal.y > 0.5f)
        {
            enemigo.Aplastar();
            veloJugador.y = Mathf.Sqrt(alturaRebote * -2f * gravedad);
            saltosHechos = 1; // tras rebotar conserva el doble salto
        }
    }

    // ---------------- MUERTE ----------------
    public void Morir()
    {
        if (!vivo) return;
        vivo = false;
        girando = false;
        StopAllCoroutines();
        if (animator != null) animator.SetTrigger("Morir");
        StartCoroutine(Reaparecer());
    }

    private IEnumerator Reaparecer()
    {
        yield return new WaitForSeconds(tiempoReaparicion);

        controller.enabled = false;
        transform.position = puntoReaparicion;
        controller.enabled = true;

        veloJugador = Vector3.zero;
        saltosHechos = 0;
        vivo = true;

        if (animator != null)
        {
            animator.Rebind();      // vuelve al estado inicial (idle/correr)
            animator.Update(0f);
        }
    }

    // ---------------- ANIMATOR ----------------
    private void ActualizarAnimator()
    {
        if (animator == null) return;
        animator.SetBool("EnPiso", enPiso);
        animator.SetFloat("Velocidad", vivo ? new Vector2(X, Z).magnitude : 0f);
    }

    // ---------------- GIZMOS (ayuda visual en la Scene) ----------------
    // Verde: esfera del CheckPiso (debe tocar el suelo).
    private void OnDrawGizmos()
    {
        if (checkPiso == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPiso.position, distanciaPiso);
    }

    // Rojo: radio del giro de ataque (solo al seleccionar el jugador).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * alturaCentroGiro, radioGiro);
    }
}