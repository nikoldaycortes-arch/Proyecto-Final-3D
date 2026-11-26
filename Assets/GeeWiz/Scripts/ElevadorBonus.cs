using UnityEngine;

namespace GeeWiz
{
    public class ElevadorBonus : MonoBehaviour
    {
        //El primer punto es abajo en el camino y el ultimo arriba en el bonus
        public Transform[] puntos;
        public Transform puntoDestinoBonus;
        public float velocidad = 9f;
        public float esperaSalida = 0.4f;
        public float esperaRegreso = 2f;
        public float radioPlataforma = 1.3f;

        private CharacterController pasajero;
        private MovoJugadorTPV jugador;
        private Collider colliderPlataforma;
        private int indice = 0;
        private int sentido = 0;
        private float tiempoEspera = 0f;
        private bool bonusActivado = false;

        void Start()
        {
            transform.position = puntos[0].position;
            colliderPlataforma = GetComponent<Collider>();

            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null)
            {
                pasajero = objJugador.GetComponent<CharacterController>();
                jugador = objJugador.GetComponent<MovoJugadorTPV>();
            }
        }

        void Update()
        {
            bool montado = EstaMontado();

            if (sentido == 0)
            {
                //Sube cuando Crash se monta y baja sola cuando ya se bajo arriba
                bool arriba = indice == puntos.Length - 1;
                if (arriba && montado && !bonusActivado)
                {
                    bonusActivado = true;
                    jugador.ConfigurarZonaBonus(true, puntoDestinoBonus.position + Vector3.up * 0.5f);
                }

                bool debeMoverse = arriba ? !montado : montado;
                tiempoEspera = debeMoverse ? tiempoEspera + Time.deltaTime : 0f;

                if (tiempoEspera >= (arriba ? esperaRegreso : esperaSalida))
                {
                    sentido = arriba ? -1 : 1;
                    if (!arriba) bonusActivado = false;
                    tiempoEspera = 0f;
                }
                return;
            }

            Vector3 objetivo = puntos[indice + sentido].position;
            Vector3 antes = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);

            if (montado)
            {
                pasajero.Move(transform.position - antes);
            }

            if (Vector3.Distance(transform.position, objetivo) < 0.01f)
            {
                indice += sentido;
                if (indice == 0 || indice == puntos.Length - 1)
                {
                    sentido = 0;
                }
            }
        }

        //Se revisa la posicion en vez de usar un trigger para que no lo suelte cuando va rapido
        private bool EstaMontado()
        {
            if (pasajero == null) return false;

            Vector3 pies = pasajero.transform.position;
            float techo = colliderPlataforma.bounds.max.y;
            Vector2 distancia = new Vector2(pies.x - transform.position.x, pies.z - transform.position.z);
            return distancia.magnitude <= radioPlataforma && pies.y >= techo - 0.4f && pies.y <= techo + 1.2f;
        }
    }
}
