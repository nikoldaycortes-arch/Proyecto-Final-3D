using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeWiz
{
    public class MovoJugadorTPV : MonoBehaviour
    {
        private CharacterController controller;

        public float veloMovi = 2f;
        public float veloRota = 10f;

        public float X, Z;

        [SerializeField]
        private Camera followCamera;

        private Vector3 veloJugador;
        public Transform checkPiso;
        public float distanciaPiso = 0.4f;
        public LayerMask piso;
        public float gravedad = -9.81f;
        public float salto = 1f;

        public float rebote = 4f;
        public float alturaCaida = -10f;
        private Vector3 puntoInicio;

        private bool enPiso;

        [Header("Correr y Salto Avanzado")]
        public float multiplicadorCorrer = 1.65f;
        public int saltosMaximos = 2;
        public float factorDobleSalto = 0.9f;
        public float tiempoCoyote = 0.12f;

        [Header("Gravedad Personalizada y Planchazo")]
        public float radioPlanchazo = 2.2f;
        public GameObject vfxPlanchazo;
        public float suspensionPlanchazo = 0.12f;
        public float gravedadPlanchazoInicial = 2f;
        public float gravedadPlanchazoFinal = 14f;
        public float rampaPlanchazo = 0.3f;
        public TrailRenderer[] estelasPlanchazo;
        private Vector3 direccionGravedad = Vector3.down;
        private float magnitudGravedad;
        private bool haciendoPlanchazo = false;
        private float inicioPlanchazo = -10f;

        [Header("Lanzar Objeto")]
        public GameObject prefabProyectil;
        public Transform puntoLanzamiento;
        public float fuerzaLanzamiento = 18f;
        public float enfriamientoLanzamiento = 0.35f;
        private float proximoLanzamiento = 0f;

        [Header("Animacion y VFX")]
        public Animator animator;
        public GameObject vfxPolvoSalto;
        public float velocidadNaturalCarrera = 3.5f;

        [Header("Aku Aku")]
        public int nivelAkuAku = 0;
        public GameObject prefabMascaraAku;

        public bool enZonaBonus { get; private set; }
        public bool EnPiso => enPiso;
        public Vector3 puntoEntradaBonus;
        public float tiempoInvulnerable = 1.2f;

        private int saltosHechos = 0;
        private float ultimoTiempoEnPiso = -10f;
        private bool estabaEnPiso = false;
        private float finInvulnerabilidad = 0f;
        private float finInvencibilidadAku = 0f;
        private Vector3 escalaBaseModelo = Vector3.one;
        private Vector3 posicionBaseModelo = Vector3.zero;
        private float inicioVoltereta = -10f;
        private float anguloPose = 0f;
        private float finAplastado = -10f;

        public float AnguloGiro { get; set; }
        private readonly HashSet<string> parametrosAnimator = new HashSet<string>();
        private GameObject mascaraAkuObj;
        private Material materialCuerpoMascara;
        private Material[] materialesModeloAku;

        private bool EstaInvencibleAku3 => nivelAkuAku >= 3 && Time.time < finInvencibilidadAku;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            puntoInicio = transform.position;
            magnitudGravedad = Mathf.Abs(gravedad);

            if (followCamera == null)
            {
                followCamera = Camera.main;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                escalaBaseModelo = animator.transform.localScale;
                posicionBaseModelo = animator.transform.localPosition;
                animator.updateMode = AnimatorUpdateMode.Normal;
            }

            CachearParametrosAnimator();
        }

        private void Update()
        {
            if (SistemaVidas.instancia != null && SistemaVidas.instancia.SinVidas)
            {
                return;
            }

            if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado))
            {
                return;
            }

            if (nivelAkuAku >= 3 && Time.time >= finInvencibilidadAku)
            {
                nivelAkuAku = 2;
                GestorJuego.Instancia?.ActualizarAkuAku(nivelAkuAku);
                ActualizarAparienciaAkuAku();
            }

            if (animator != null)
            {
                animator.transform.localScale = Vector3.Lerp(animator.transform.localScale, escalaBaseModelo, 12f * Time.deltaTime);
                ActualizarPoseModelo();
            }

            Movimiento();
            ProcesarLanzamiento();
            ActualizarVisualAkuAku();

            if (transform.position.y < alturaCaida)
            {
                if (enZonaBonus)
                {
                    Teletransportar(puntoEntradaBonus);
                }
                else
                {
                    SistemaVidas.instancia?.DescontarVida();
                    GestorJuego.Instancia?.RestarVida();

                    bool sinVidas = (SistemaVidas.instancia != null && SistemaVidas.instancia.SinVidas)
                                    || (GestorJuego.Instancia != null && GestorJuego.Instancia.juegoTerminado);

                    if (sinVidas)
                    {
                        if (animator != null)
                        {
                            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                        }
                        DispararTriggerAnimator("Morir");
                    }
                    else
                    {
                        Reaparecer();
                    }
                }
            }
        }

        public void Movimiento()
        {
            Vector3 dirArriba = -direccionGravedad.normalized;

            if (checkPiso != null)
            {
                enPiso = Physics.CheckSphere(checkPiso.position, distanciaPiso, piso);
            }
            else
            {
                enPiso = controller.isGrounded;
            }

            if (enPiso)
            {
                ultimoTiempoEnPiso = Time.time;

                if (!estabaEnPiso && haciendoPlanchazo)
                {
                    EjecutarImpactoPlanchazo();
                }

                haciendoPlanchazo = false;
                saltosHechos = 0;

                if (Vector3.Dot(veloJugador, dirArriba) < 0f)
                {
                    veloJugador = -dirArriba * 2f;
                }
            }

            estabaEnPiso = enPiso;

            X = Input.GetAxis("Horizontal");
            Z = Input.GetAxis("Vertical");

            float yawCamara = followCamera != null ? followCamera.transform.eulerAngles.y : transform.eulerAngles.y;

            Vector3 moveInput = Quaternion.Euler(0, yawCamara, 0) * new Vector3(X, 0, Z);
            Vector3 moveDirection = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;

            bool corriendo = Input.GetKey(KeyCode.LeftShift);
            float velocidadActual = veloMovi * (corriendo ? multiplicadorCorrer : 1f);
            if (haciendoPlanchazo)
            {
                moveDirection = Vector3.zero;
            }

            controller.Move(moveDirection * velocidadActual * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion rotacion = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, veloRota * Time.deltaTime);
            }

            bool puedePrimerSalto = enPiso || (Time.time - ultimoTiempoEnPiso <= tiempoCoyote && saltosHechos == 0);
            if (Input.GetButtonDown("Jump") && (puedePrimerSalto || saltosHechos < saltosMaximos))
            {
                haciendoPlanchazo = false;
                if (!puedePrimerSalto && saltosHechos == 0)
                {
                    saltosHechos = 1;
                }
                float alturaObjetivo = saltosHechos == 0 ? salto : salto * factorDobleSalto;
                float v0 = Mathf.Sqrt(2f * magnitudGravedad * Mathf.Max(0.1f, alturaObjetivo));
                veloJugador = dirArriba * v0;
                saltosHechos++;
                ultimoTiempoEnPiso = -10f;
                if (saltosHechos == 2)
                {
                    inicioVoltereta = Time.time;
                }

                DispararTriggerAnimator("Saltar");
                if (animator != null)
                {
                    animator.transform.localScale = Vector3.Scale(escalaBaseModelo, new Vector3(0.86f, 1.18f, 0.86f));
                }
                if (vfxPolvoSalto != null)
                {
                    Instantiate(vfxPolvoSalto, transform.position, Quaternion.identity);
                }
                GestorJuego.Instancia?.ReproducirSFX("jump");
            }

            if (!enPiso && !haciendoPlanchazo && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C)))
            {
                haciendoPlanchazo = true;
                inicioPlanchazo = Time.time;
                veloJugador = Vector3.zero;
            }

            float gravedadAplicada = magnitudGravedad;
            if (haciendoPlanchazo)
            {
                float tiempoCaida = Time.time - inicioPlanchazo - suspensionPlanchazo;
                if (tiempoCaida < 0f)
                {
                    veloJugador = Vector3.zero;
                    gravedadAplicada = 0f;
                }
                else
                {
                    float curva = Mathf.Clamp01(tiempoCaida / rampaPlanchazo);
                    gravedadAplicada = magnitudGravedad * Mathf.Lerp(gravedadPlanchazoInicial, gravedadPlanchazoFinal, curva * curva * curva);
                }
            }
            veloJugador += direccionGravedad.normalized * gravedadAplicada * Time.deltaTime;
            controller.Move(veloJugador * Time.deltaTime);

            ActualizarAnimaciones(moveDirection.magnitude * (corriendo ? multiplicadorCorrer : 1f), corriendo);
        }

        private void ProcesarLanzamiento()
        {
            if (!(Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.F)))
            {
                return;
            }
            if (Time.time < proximoLanzamiento)
            {
                return;
            }

            proximoLanzamiento = Time.time + enfriamientoLanzamiento;
            Vector3 origen = puntoLanzamiento != null ? puntoLanzamiento.position : transform.position + transform.forward * 0.9f + Vector3.up * 0.9f;
            Vector3 direccion = (transform.forward + Vector3.up * 0.15f).normalized;

            GameObject objProyectil;
            if (prefabProyectil != null)
            {
                objProyectil = Instantiate(prefabProyectil, origen, Quaternion.LookRotation(direccion));
            }
            else
            {
                objProyectil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                objProyectil.transform.position = origen;
                objProyectil.transform.localScale = Vector3.one * 0.35f;
                Renderer rend = objProyectil.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(1f, 0.45f, 0.05f);
                }
                SphereCollider col = objProyectil.GetComponent<SphereCollider>();
                if (col != null)
                {
                    col.isTrigger = true;
                }
            }

            ProyectilWumpa proyectil = objProyectil.GetComponent<ProyectilWumpa>();
            if (proyectil == null)
            {
                proyectil = objProyectil.AddComponent<ProyectilWumpa>();
            }
            proyectil.Inicializar(direccion, fuerzaLanzamiento);
            GestorJuego.Instancia?.ReproducirSFX("wumpa");
        }

        private void EjecutarImpactoPlanchazo()
        {
            Collider[] impactos = Physics.OverlapSphere(transform.position, radioPlanchazo);
            HashSet<IGolpeable> golpeados = new HashSet<IGolpeable>();

            foreach (Collider col in impactos)
            {
                if (col.transform == transform || col.transform.IsChildOf(transform))
                {
                    continue;
                }

                IGolpeable golpeable = col.GetComponent<IGolpeable>() ?? col.GetComponentInParent<IGolpeable>();
                if (golpeable != null && !golpeados.Contains(golpeable))
                {
                    golpeados.Add(golpeable);
                    golpeable.Golpear();
                }
            }

            if (animator != null)
            {
                animator.transform.localScale = Vector3.Scale(escalaBaseModelo, new Vector3(1.6f, 1.35f, 0.3f));
                finAplastado = Time.time + 0.5f;
            }

            if (vfxPlanchazo != null)
            {
                Instantiate(vfxPlanchazo, transform.position, Quaternion.identity);
            }
            GestorJuego.Instancia?.ReproducirSFX("crate");
        }

        public void CambiarGravedad(Vector3 nuevaDireccion, float nuevaMagnitud)
        {
            if (nuevaDireccion.sqrMagnitude > 0.001f)
            {
                direccionGravedad = nuevaDireccion.normalized;
            }
            magnitudGravedad = Mathf.Max(0.1f, Mathf.Abs(nuevaMagnitud));
        }

        public void RestaurarGravedad()
        {
            direccionGravedad = Vector3.down;
            magnitudGravedad = Mathf.Abs(gravedad);
        }

        public void ActualizarCheckpoint(Vector3 nuevaPosicion)
        {
            puntoInicio = nuevaPosicion + Vector3.up * 0.6f;
        }

        public void ConfigurarZonaBonus(bool activa, Vector3 entradaBonus)
        {
            enZonaBonus = activa;
            puntoEntradaBonus = entradaBonus;
            if (followCamera != null)
            {
                followCamera.GetComponent<Camara>()?.ConfigurarVistaBonus(activa);
            }
        }

        public void Impulsar(float velocidadVertical)
        {
            Vector3 dirArriba = -direccionGravedad.normalized;
            veloJugador = dirArriba * velocidadVertical;
            haciendoPlanchazo = false;
            saltosHechos = 1;
            if (animator != null)
            {
                animator.transform.localScale = Vector3.Scale(escalaBaseModelo, new Vector3(0.86f, 1.18f, 0.86f));
            }
        }

        public void OtorgarAkuAku()
        {
            nivelAkuAku = Mathf.Min(3, nivelAkuAku + 1);
            if (nivelAkuAku == 3)
            {
                finInvencibilidadAku = Time.time + 8f;
            }

            GestorJuego.Instancia?.ActualizarAkuAku(nivelAkuAku);
            CrearVisualAkuAkuSiFalta();
            ActualizarAparienciaAkuAku();
        }

        private void CrearVisualAkuAkuSiFalta()
        {
            if (mascaraAkuObj != null)
            {
                return;
            }

            if (prefabMascaraAku != null)
            {
                mascaraAkuObj = Instantiate(prefabMascaraAku, transform);
                List<Material> materiales = new List<Material>();
                foreach (Renderer r in mascaraAkuObj.GetComponentsInChildren<Renderer>())
                {
                    materiales.AddRange(r.materials);
                }
                materialesModeloAku = materiales.ToArray();
                return;
            }

            mascaraAkuObj = new GameObject("Mascara_AkuAku");
            mascaraAkuObj.transform.SetParent(transform, false);

            GameObject tabla = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabla.transform.SetParent(mascaraAkuObj.transform, false);
            tabla.transform.localScale = new Vector3(0.36f, 0.48f, 0.12f);
            Destroy(tabla.GetComponent<Collider>());

            Renderer rendTabla = tabla.GetComponent<Renderer>();
            if (rendTabla != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                materialCuerpoMascara = shader != null ? new Material(shader) : new Material(rendTabla.sharedMaterial);
                rendTabla.material = materialCuerpoMascara;
            }

            Color colorOjo = new Color(1f, 0.9f, 0.15f);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                GameObject ojo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ojo.transform.SetParent(mascaraAkuObj.transform, false);
                ojo.transform.localPosition = new Vector3(lado * 0.08f, 0.06f, 0.06f);
                ojo.transform.localScale = new Vector3(0.08f, 0.08f, 0.05f);
                Destroy(ojo.GetComponent<Collider>());
                Renderer rOjo = ojo.GetComponent<Renderer>();
                if (rOjo != null)
                {
                    if (rOjo.material.HasProperty("_BaseColor"))
                    {
                        rOjo.material.SetColor("_BaseColor", colorOjo);
                    }
                    rOjo.material.color = colorOjo;
                }
            }

            Color[] coloresPluma = { new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.35f), new Color(0.95f, 0.78f, 0.15f) };
            for (int i = 0; i < 3; i++)
            {
                GameObject pluma = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pluma.transform.SetParent(mascaraAkuObj.transform, false);
                pluma.transform.localPosition = new Vector3((i - 1) * 0.09f, 0.3f, 0f);
                pluma.transform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 18f);
                pluma.transform.localScale = new Vector3(0.07f, 0.18f, 0.04f);
                Destroy(pluma.GetComponent<Collider>());
                Renderer rPluma = pluma.GetComponent<Renderer>();
                if (rPluma != null)
                {
                    if (rPluma.material.HasProperty("_BaseColor"))
                    {
                        rPluma.material.SetColor("_BaseColor", coloresPluma[i]);
                    }
                    rPluma.material.color = coloresPluma[i];
                }
            }
        }

        private void ActualizarAparienciaAkuAku()
        {
            if (mascaraAkuObj == null)
            {
                return;
            }
            mascaraAkuObj.SetActive(nivelAkuAku > 0);

            if (materialesModeloAku != null)
            {
                float brillo = nivelAkuAku >= 3 ? 1.2f : (nivelAkuAku == 2 ? 0.35f : 0f);
                foreach (Material mat in materialesModeloAku)
                {
                    if (brillo > 0f)
                    {
                        mat.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                    mat.SetColor("_EmissionColor", new Color(1f, 0.82f, 0.18f) * brillo);
                }
                return;
            }

            if (materialCuerpoMascara == null || nivelAkuAku <= 0)
            {
                return;
            }

            Color colorMascara = nivelAkuAku == 1
                ? new Color(0.55f, 0.32f, 0.14f)
                : new Color(1f, 0.78f, 0.15f);

            if (materialCuerpoMascara.HasProperty("_BaseColor"))
            {
                materialCuerpoMascara.SetColor("_BaseColor", colorMascara);
            }
            materialCuerpoMascara.color = colorMascara;

            if (nivelAkuAku >= 2)
            {
                materialCuerpoMascara.EnableKeyword("_EMISSION");
                float intensidad = nivelAkuAku >= 3 ? 2.4f : 0.7f;
                if (materialCuerpoMascara.HasProperty("_EmissionColor"))
                {
                    materialCuerpoMascara.SetColor("_EmissionColor", new Color(1f, 0.82f, 0.18f) * intensidad);
                }
            }
            else
            {
                materialCuerpoMascara.DisableKeyword("_EMISSION");
                if (materialCuerpoMascara.HasProperty("_EmissionColor"))
                {
                    materialCuerpoMascara.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private void ActualizarVisualAkuAku()
        {
            if (mascaraAkuObj == null)
            {
                return;
            }
            if (nivelAkuAku <= 0)
            {
                mascaraAkuObj.SetActive(false);
                return;
            }

            if (!mascaraAkuObj.activeSelf)
            {
                mascaraAkuObj.SetActive(true);
            }
            mascaraAkuObj.transform.localPosition = new Vector3(
                0.62f + Mathf.Cos(Time.time * 2.4f) * 0.12f,
                1.35f + Mathf.Sin(Time.time * 3.5f) * 0.14f,
                -0.1f + Mathf.Sin(Time.time * 2.4f) * 0.12f
            );
            mascaraAkuObj.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 2f) * 18f, Mathf.Cos(Time.time * 2.5f) * 6f);
            float escala = nivelAkuAku >= 3 ? 1.15f + Mathf.Sin(Time.time * 12f) * 0.08f : 1f;
            mascaraAkuObj.transform.localScale = Vector3.one * escala;
        }

        public void RecibirDano()
        {
            if (EstaInvencibleAku3)
            {
                return;
            }
            if (Time.time < finInvulnerabilidad)
            {
                return;
            }
            if (SistemaVidas.instancia != null && SistemaVidas.instancia.SinVidas)
            {
                return;
            }
            if (GestorJuego.Instancia != null && GestorJuego.Instancia.juegoTerminado)
            {
                return;
            }

            if (!enZonaBonus && nivelAkuAku > 0)
            {
                nivelAkuAku--;
                finInvulnerabilidad = Time.time + 1.5f;
                GestorJuego.Instancia?.ActualizarAkuAku(nivelAkuAku);
                ActualizarAparienciaAkuAku();
                GestorJuego.Instancia?.ReproducirSFX("hit");
                if (animator != null)
                {
                    animator.transform.localScale = Vector3.Scale(escalaBaseModelo, new Vector3(1.25f, 0.75f, 1.25f));
                }
                return;
            }

            finInvulnerabilidad = Time.time + tiempoInvulnerable;

            if (enZonaBonus)
            {
                GestorJuego.Instancia?.ReproducirSFX("hit");
                Teletransportar(puntoEntradaBonus);
                return;
            }

            DispararTriggerAnimator("Morir");
            SistemaVidas.instancia?.DescontarVida();
            GestorJuego.Instancia?.RestarVida();

            bool sinVidas = (SistemaVidas.instancia != null && SistemaVidas.instancia.SinVidas)
                            || (GestorJuego.Instancia != null && GestorJuego.Instancia.juegoTerminado);

            if (sinVidas)
            {
                if (animator != null)
                {
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                }
            }
            else
            {
                Teletransportar(puntoInicio);
            }
        }

        public void Teletransportar(Vector3 destino)
        {
            veloJugador = Vector3.zero;
            haciendoPlanchazo = false;
            EncenderEstelas(false);
            RestaurarGravedad();

            controller.enabled = false;
            transform.position = destino;
            controller.enabled = true;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.transform.localScale = escalaBaseModelo;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            float velVertical = Vector3.Dot(veloJugador, -direccionGravedad.normalized);

            Caja caja = hit.collider.GetComponent<Caja>() ?? hit.collider.GetComponentInParent<Caja>();
            if (caja != null && caja.esContornoFantasma)
            {
                return;
            }

            if (caja != null && caja.tipo == Caja.TipoCaja.Nitro)
            {
                caja.Explotar();
                return;
            }

            if (caja != null && hit.normal.y > 0.5f && velVertical <= 0.1f)
            {
                if (caja.tipo == Caja.TipoCaja.TNT)
                {
                    caja.ActivarCuentaRegresivaTNT();
                    caja.AnimarRebote();
                    Impulsar(rebote);
                    return;
                }

                if (caja.tipo == Caja.TipoCaja.MetalicaRebote)
                {
                    caja.AnimarRebote();
                    Impulsar(rebote * 1.4f);
                    return;
                }

                if (caja.tipo == Caja.TipoCaja.InterruptorNitro || caja.tipo == Caja.TipoCaja.InterruptorMetal)
                {
                    if (!caja.YaActivada)
                    {
                        caja.Golpear();
                        Impulsar(rebote * 0.85f);
                    }
                    return;
                }
            }

            if (caja != null && hit.normal.y < -0.5f && velVertical > 0.1f)
            {
                if (veloJugador.y > 0f)
                {
                    veloJugador.y = -1.5f;
                }

                if (caja.tipo == Caja.TipoCaja.MetalicaRebote)
                {
                    caja.AnimarRebote();
                    return;
                }

                if (caja.tipo == Caja.TipoCaja.InterruptorNitro || caja.tipo == Caja.TipoCaja.InterruptorMetal)
                {
                    if (!caja.YaActivada)
                    {
                        caja.Golpear();
                    }
                    return;
                }

                if (caja.tipo == Caja.TipoCaja.TNT)
                {
                    caja.ActivarCuentaRegresivaTNT();
                    caja.AnimarRebote();
                    return;
                }

                caja.Golpear();
                return;
            }

            IGolpeable golpeable = hit.collider.GetComponent<IGolpeable>() ?? hit.collider.GetComponentInParent<IGolpeable>();
            if (golpeable == null)
            {
                return;
            }

            if (velVertical <= 0.1f && hit.normal.y > 0.5f)
            {
                golpeable.Golpear();
                Impulsar(rebote);
            }
        }

        private void Reaparecer()
        {
            Teletransportar(puntoInicio);
        }

        private void CachearParametrosAnimator()
        {
            parametrosAnimator.Clear();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                parametrosAnimator.Add(param.name);
            }
        }

        private void ActualizarAnimaciones(float velocidadNormalizada, bool corriendo)
        {
            if (animator == null)
            {
                return;
            }

            if (parametrosAnimator.Contains("Velocidad"))
            {
                animator.SetFloat("Velocidad", velocidadNormalizada);
            }
            if (parametrosAnimator.Contains("EnPiso"))
            {
                animator.SetBool("EnPiso", enPiso);
            }
            if (parametrosAnimator.Contains("MultiplicadorVel"))
            {
                float velocidadReal = velocidadNormalizada * veloMovi;
                if (parametrosAnimator.Contains("Planchazo"))
                {
                    animator.SetBool("Planchazo", haciendoPlanchazo || Time.time < finAplastado);
                }
                animator.SetFloat("MultiplicadorVel", Mathf.Max(0.5f, velocidadReal / velocidadNaturalCarrera));
            }
        }

        private void DispararTriggerAnimator(string nombreTrigger)
        {
            if (animator != null && parametrosAnimator.Contains(nombreTrigger))
            {
                animator.SetTrigger(nombreTrigger);
            }
        }

        private void ActualizarPoseModelo()
        {
            bool tumbado = haciendoPlanchazo || Time.time < finAplastado;
            float progreso = (Time.time - inicioVoltereta) / 0.45f;
            float ladeo = 0f;
            float desvio = 0f;
            if (haciendoPlanchazo)
            {
                float t = Time.time - inicioPlanchazo;
                float amortiguado = Mathf.Exp(-7f * t);
                anguloPose = 90f - 115f * amortiguado * Mathf.Cos(22f * t);
                ladeo = 25f * amortiguado * Mathf.Sin(37f * t);
                desvio = 12f * amortiguado * Mathf.Sin(29f * t + 1f);
            }
            else if (progreso < 1f)
            {
                anguloPose = Mathf.SmoothStep(0f, 360f, progreso);
            }
            else
            {
                anguloPose = Mathf.MoveTowardsAngle(anguloPose, tumbado ? 90f : 0f, 720f * Time.deltaTime);
            }

            EncenderEstelas(haciendoPlanchazo && Time.time - inicioPlanchazo > suspensionPlanchazo);

            if (haciendoPlanchazo)
            {
                Vector3 estirado = Vector3.Scale(escalaBaseModelo, new Vector3(0.8f, 0.85f, 1.45f));
                animator.transform.localScale = Vector3.Lerp(animator.transform.localScale, estirado, 15f * Time.deltaTime);
            }

            Vector3 centro = Vector3.up * 0.8f;
            Quaternion giro = Quaternion.Euler(0f, AnguloGiro + desvio, 0f) * Quaternion.Euler(anguloPose, 0f, ladeo);
            animator.transform.localRotation = giro;
            float bajada = Time.time < finAplastado ? 0.6f * Mathf.Clamp01(anguloPose / 90f) : 0f;
            animator.transform.localPosition = posicionBaseModelo + centro - giro * centro + Vector3.down * bajada;
        }

        private void EncenderEstelas(bool encender)
        {
            if (estelasPlanchazo == null)
            {
                return;
            }
            foreach (TrailRenderer estela in estelasPlanchazo)
            {
                if (estela == null)
                {
                    continue;
                }
                if (encender && !estela.emitting)
                {
                    estela.Clear();
                }
                estela.emitting = encender;
            }
        }
    }
}