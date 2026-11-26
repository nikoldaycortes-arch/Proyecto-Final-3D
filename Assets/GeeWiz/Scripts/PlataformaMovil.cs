using UnityEngine;

namespace GeeWiz
{
    public class PlataformaMovil : MonoBehaviour
    {
        public Transform puntoA;
        public Transform puntoB;
        public float velocidad = 3f;

        private Transform destino;
        private CharacterController pasajero;

        void Start()
        {
            destino = puntoB;
        }

    void Update()
        {
            Vector3 antes = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, destino.position, velocidad * Time.deltaTime);

            //Al jugador se le aplica el mismo desplazamiento para que viaje encima
            if (pasajero != null)
            {
                pasajero.Move(transform.position - antes);
            }

            if (Vector3.Distance(transform.position, destino.position) < 0.05f)
            {
                if (destino == puntoB)
                {
                    destino = puntoA;
                }
                else
                {
                    destino = puntoB;
                }
            }
        }

    private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                pasajero = other.GetComponent<CharacterController>();
            }
        }

    private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                pasajero = null;
            }
        }
    }
}
