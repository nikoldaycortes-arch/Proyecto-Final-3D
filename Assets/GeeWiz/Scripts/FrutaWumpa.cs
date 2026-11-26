using UnityEngine;

namespace GeeWiz
{
    public class FrutaWumpa : MonoBehaviour
    {
        public bool esRelojTimeTrial = false;
        public bool esGema = false;
        public bool esCristal = false;
        public int valorWumpa = 1;
        public float velocidadRotacion = 90f;
        public float radioIman = 3.2f;
        public GameObject vfxRecoger;

        public float velocidadIman = 8.5f;

        private Transform jugador;
        private bool recogida = false;
        private Vector3 velocidadSalida;
        private float finSalida = -1f;

        private void Start()
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null)
            {
                jugador = objJugador.transform;
            }
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.World);

            if (Time.time < finSalida)
            {
                velocidadSalida += Physics.gravity * Time.deltaTime;
                transform.position += velocidadSalida * Time.deltaTime;
                return;
            }

            if (!esRelojTimeTrial && jugador != null)
            {
                float rangoIman = (esCristal || esGema) ? Mathf.Min(radioIman, 1.6f) : radioIman;
                float dist = Vector3.Distance(transform.position, jugador.position + Vector3.up * 0.8f);
                if (dist <= rangoIman)
                {
                    transform.position = Vector3.MoveTowards(transform.position, jugador.position + Vector3.up * 0.8f, velocidadIman * Time.deltaTime);
                }
            }
        }

        //Sale disparada de la caja y despues vuela sola hacia Crash como en el juego original
        public void Expulsar(Vector3 velocidad, float duracion)
        {
            velocidadSalida = velocidad;
            finSalida = Time.time + duracion;
            radioIman = 60f;
            velocidadIman = 16f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (recogida || !other.CompareTag("Player")) return;
            recogida = true;

            if (GestorJuego.Instancia != null)
            {
                if (esRelojTimeTrial)
                {
                    GestorJuego.Instancia.IniciarTimeTrial();
                }
                else if (esCristal)
                {
                    GestorJuego.Instancia.ObtenerCristal();
                }
                else if (esGema)
                {
                    GestorJuego.Instancia.ObtenerGema();
                }
                else
                {
                    GestorJuego.Instancia.SumarWumpa(valorWumpa);
                }
            }

            if (vfxRecoger != null)
            {
                Instantiate(vfxRecoger, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
