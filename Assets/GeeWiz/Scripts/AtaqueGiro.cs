using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeWiz
{
    public class AtaqueGiro : MonoBehaviour
    {
        [Header("Giro")]
        public Transform modelo;
        public float duracion = 0.5f;
        public float enfriamiento = 0.3f;
        public float radio = 1.6f;
        public float velocidadRotacion = 1440f;
        public KeyCode teclaAlternativa = KeyCode.E;
        public GameObject vfxGiro;
        //La estela queda horizontal a la altura y al radio de las puntas de los dedos
        public float alturaEstela = 1.04f;
        public float radioEstela = 1.28f;
        public float intervaloVfxGiro = 0.12f;
        public GameObject vfxChispasEscudo;
        public Animator animator;

        public bool EstaGirando => girando;

        private bool girando = false;
        private float proximoGiro = 0f;
        private Quaternion rotacionInicialModelo = Quaternion.identity;
        private bool tieneTriggerGirar = false;
        private bool tieneBoolGirando = false;
        private MovoJugadorTPV jugador;

        private void Start()
        {
            jugador = GetComponent<MovoJugadorTPV>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    if (p.name == "Girar") tieneTriggerGirar = true;
                    if (p.name == "Girando") tieneBoolGirando = true;
                }
            }

            if (modelo != null)
            {
                rotacionInicialModelo = modelo.localRotation;
            }
        }

        private void Update()
        {
            if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado))
            {
                return;
            }

            bool ctrlEnAire = Input.GetKeyDown(KeyCode.LeftControl) && jugador != null && !jugador.EnPiso;
            bool presionoGiro = (Input.GetButtonDown("Fire1") && !ctrlEnAire) || Input.GetKeyDown(teclaAlternativa) || Input.GetKeyDown(KeyCode.Q);
            if (presionoGiro && !girando && Time.time >= proximoGiro)
            {
                StartCoroutine(Girar());
            }
        }

        private IEnumerator Girar()
        {
            girando = true;
            float tiempo = 0f;
            bool chispasMostradas = false;
            HashSet<IGolpeable> golpeados = new HashSet<IGolpeable>();

            GestorJuego.Instancia?.ReproducirSFX("spin");
            if (tieneTriggerGirar && animator != null)
            {
                animator.SetTrigger("Girar");
            }
            //El estado de giro dura lo mismo que el ataque
            if (tieneBoolGirando && animator != null)
            {
                animator.SetBool("Girando", true);
            }
            float proximaEstela = 0f;

            bool rotarPorCodigo = !tieneTriggerGirar || animator == null;
            while (tiempo < duracion)
            {
                if (jugador != null)
                {
                    jugador.AnguloGiro += velocidadRotacion * Time.deltaTime;
                }
                else if (rotarPorCodigo && modelo != null)
                {
                    modelo.Rotate(0f, velocidadRotacion * Time.deltaTime, 0f);
                }

                if (vfxGiro != null && tiempo >= proximaEstela)
                {
                    //La malla de la estela esta en el plano XY, por eso se acuesta 90 grados
                    float rumbo = jugador != null ? jugador.AnguloGiro : 0f;
                    GameObject estela = Instantiate(vfxGiro, transform.position + Vector3.up * alturaEstela, Quaternion.Euler(90f, rumbo, 0f), transform);
                    estela.transform.localScale = Vector3.one * radioEstela;
                    proximaEstela += intervaloVfxGiro;
                }

                Vector3 centro = transform.position + Vector3.up * 0.8f;
                Collider[] cercanos = Physics.OverlapSphere(centro, radio);
                foreach (Collider col in cercanos)
                {
                    if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                    ProyectilWumpa proyectil = col.GetComponent<ProyectilWumpa>() ?? col.GetComponentInParent<ProyectilWumpa>();
                    if (proyectil != null && proyectil.esHechizoEnemigo)
                    {
                        if (vfxChispasEscudo != null)
                        {
                            Instantiate(vfxChispasEscudo, proyectil.transform.position, Quaternion.identity);
                        }
                        GestorJuego.Instancia?.ReproducirSFX("crate");
                        Destroy(proyectil.gameObject);
                        continue;
                    }

                    Enemigo enemigo = col.GetComponentInParent<Enemigo>();
                    if (enemigo != null && !enemigo.PuedeRecibirGiroDesde(transform.position))
                    {
                        if (!chispasMostradas)
                        {
                            chispasMostradas = true;
                            enemigo.NotificarBloqueoEscudo();
                            if (vfxChispasEscudo != null)
                            {
                                Instantiate(vfxChispasEscudo, col.ClosestPoint(centro), Quaternion.identity);
                            }
                            GestorJuego.Instancia?.ReproducirSFX("crate");
                        }
                        continue;
                    }

                    IGolpeable golpeable = col.GetComponent<IGolpeable>() ?? col.GetComponentInParent<IGolpeable>();
                    if (golpeable != null && !golpeados.Contains(golpeable))
                    {
                        golpeados.Add(golpeable);
                        golpeable.Golpear();
                    }
                }

                tiempo += Time.deltaTime;
                yield return null;
            }

            if (jugador != null)
            {
                jugador.AnguloGiro = 0f;
            }
            else if (modelo != null)
            {
                modelo.localRotation = rotacionInicialModelo;
            }

            if (tieneBoolGirando && animator != null)
            {
                animator.SetBool("Girando", false);
            }

            girando = false;
            proximoGiro = Time.time + enfriamiento;
        }
    }
}
