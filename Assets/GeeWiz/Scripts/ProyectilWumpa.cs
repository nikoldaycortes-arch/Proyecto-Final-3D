using UnityEngine;

namespace GeeWiz
{
    public class ProyectilWumpa : MonoBehaviour
    {
        public float velocidad = 18f;
        public float gravedad = -12f;
        public float tiempoVida = 2.5f;
        public GameObject vfxImpacto;
        public bool esHechizoEnemigo = false;
        public GameObject emisor;

        private Vector3 velocidadActual;
        private bool inicializado = false;

        public void Inicializar(Vector3 direccion, float fuerza)
        {
            velocidadActual = direccion.normalized * fuerza;
            inicializado = true;
        }

        public void InicializarHechizo(Vector3 direccion, float fuerza, GameObject origen)
        {
            esHechizoEnemigo = true;
            gravedad = 0f;
            emisor = origen;
            Inicializar(direccion, fuerza);
        }

        private void Start()
        {
            //Le ponemos rigidbody para que detecte bien las colisiones
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!inicializado)
            {
                velocidadActual = transform.forward * velocidad;
            }

            Destroy(gameObject, tiempoVida);
        }

        private void Update()
        {
            velocidadActual.y += gravedad * Time.deltaTime;
            transform.position += velocidadActual * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            if (emisor != null && (other.gameObject == emisor || other.transform.IsChildOf(emisor.transform))) return;

            if (esHechizoEnemigo)
            {
                if (other.GetComponentInParent<Enemigo>() != null) return;

                if (other.CompareTag("Player"))
                {
                    AtaqueGiro giro = other.GetComponent<AtaqueGiro>() ?? other.GetComponentInParent<AtaqueGiro>();
                    if (giro == null || !giro.EstaGirando)
                    {
                        MovoJugadorTPV jugador = other.GetComponent<MovoJugadorTPV>() ?? other.GetComponentInParent<MovoJugadorTPV>();
                        jugador?.RecibirDano();
                    }
                    else
                    {
                        GestorJuego.Instancia?.ReproducirSFX("crate");
                    }
                }

                if (vfxImpacto != null)
                {
                    Instantiate(vfxImpacto, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
                return;
            }

            //Para que no choque con el mismo jugador al disparar
            if (other.CompareTag("Player")) return;

            IGolpeable golpeable = other.GetComponent<IGolpeable>() ?? other.GetComponentInParent<IGolpeable>();
            if (golpeable != null)
            {
                golpeable.Golpear();
            }

            if (vfxImpacto != null)
            {
                Instantiate(vfxImpacto, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
