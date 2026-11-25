using System.Collections;
using UnityEngine;

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
    [SerializeField] private Transform huesoRaizHips;
    [SerializeField] private Transform brazoIzquierdo;
    [SerializeField] private Transform brazoDerecho;

    [Header("Piso")]
    public Transform checkPiso;
    public float distanciaPiso = 0.4f;
    public LayerMask piso;
    public float gravedad = -25f;

    [Header("Salto")]
    public float salto = 1.6f;
    public int saltosMaximos = 2;
    [Range(0.5f, 1.2f)]
    public float factorDobleSalto = 0.9f;
    public float tiempoCoyote = 0.1f;
    public float alturaRebote = 1.0f;

    [Header("Giro de ataque")]
    public float duracionGiro = 0.5f;
    public float cooldownGiro = 0.2f;
    public float radioGiro = 1.6f;
    public float alturaCentroGiro = 1f;
    public float velocidadGiroVisual = 2400f;
    public Vector3 rotacionBrazoIzquierdo = Vector3.zero;
    public Vector3 rotacionBrazoDerecho = Vector3.zero;

    [Header("Muerte")]
    public float tiempoReaparicion = 2f;
    public Vector3 puntoReaparicion;

    private Vector3 veloJugador;
    private bool enPiso;
    private int saltosHechos;
    private float ultimoTiempoPiso;
    private bool girando;
    private float proximoGiro;
    private bool vivo = true;
    private float anguloGiroVisual;

    public bool EstaVivo => vivo;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (followCamera == null)
        {
            followCamera = Camera.main;
        }
        if (huesoRaizHips == null)
        {
            huesoRaizHips = transform.Find("mixamorig:Hips");
        }
        if (brazoIzquierdo == null)
        {
            brazoIzquierdo = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm");
        }
        if (brazoDerecho == null)
        {
            brazoDerecho = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm");
        }
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

    private void LateUpdate()
    {
        if (girando && huesoRaizHips != null)
        {
            if (brazoIzquierdo != null)
            {
                brazoIzquierdo.localRotation = Quaternion.Euler(rotacionBrazoIzquierdo);
            }
            if (brazoDerecho != null)
            {
                brazoDerecho.localRotation = Quaternion.Euler(rotacionBrazoDerecho);
            }

            anguloGiroVisual += velocidadGiroVisual * Time.deltaTime;
            huesoRaizHips.Rotate(transform.up, anguloGiroVisual, Space.World);
        }
    }

    private void Movimiento()
    {
        enPiso = controller.isGrounded ||
                 (checkPiso != null && Physics.CheckSphere(checkPiso.position, distanciaPiso, piso));

        if (enPiso && veloJugador.y <= 0f)
        {
            veloJugador.y = -2f;
            saltosHechos = 0;
            ultimoTiempoPiso = Time.time;
        }

        X = Input.GetAxisRaw("Horizontal");
        Z = Input.GetAxisRaw("Vertical");

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
        if (Input.GetButtonDown("Jump"))
        {
            IntentarSalto();
        }

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
            if (saltosHechos == 0)
            {
                saltosHechos = 1;
            }
            EjecutarSalto(factorDobleSalto);
            saltosHechos++;
        }
    }

    private void EjecutarSalto(float factor)
    {
        veloJugador.y = Mathf.Sqrt(salto * -2f * gravedad) * factor;
        if (animator != null)
        {
            animator.SetTrigger("Saltar");
        }
    }

    private IEnumerator Girar()
    {
        girando = true;
        anguloGiroVisual = 0f;
        if (animator != null)
        {
            animator.SetBool("Girando", true);
        }

        float t = 0f;
        while (t < duracionGiro)
        {
            t += Time.deltaTime;
            GolpearEnemigos();
            yield return null;
        }

        girando = false;
        anguloGiroVisual = 0f;
        if (animator != null)
        {
            animator.SetBool("Girando", false);
        }
        proximoGiro = Time.time + cooldownGiro;
    }

    private void GolpearEnemigos()
    {
        Vector3 centro = transform.position + Vector3.up * alturaCentroGiro;
        Collider[] cercanos = Physics.OverlapSphere(centro, radioGiro);
        foreach (Collider c in cercanos)
        {
            Enemigo enemigo = c.GetComponentInParent<Enemigo>();
            if (enemigo != null && enemigo.estaVivo)
            {
                enemigo.Morir();
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Enemigo enemigo = hit.collider.GetComponentInParent<Enemigo>();
        if (enemigo == null || !enemigo.estaVivo)
        {
            return;
        }

        if (veloJugador.y <= 0f && hit.normal.y > 0.5f)
        {
            enemigo.Aplastar();
            veloJugador.y = Mathf.Sqrt(alturaRebote * -2f * gravedad);
            saltosHechos = 1;
        }
    }

    public void Morir()
    {
        if (!vivo)
        {
            return;
        }
        vivo = false;
        girando = false;
        anguloGiroVisual = 0f;
        StopAllCoroutines();
        if (animator != null)
        {
            animator.SetBool("Girando", false);
            animator.SetTrigger("Morir");
        }
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
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void ActualizarAnimator()
    {
        if (animator == null)
        {
            return;
        }
        animator.SetBool("EnPiso", enPiso);
        animator.SetFloat("Velocidad", vivo ? new Vector2(X, Z).magnitude : 0f);
    }

    private void OnDrawGizmos()
    {
        if (checkPiso == null)
        {
            return;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPiso.position, distanciaPiso);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * alturaCentroGiro, radioGiro);
    }
}