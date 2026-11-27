using UnityEngine;

namespace GeeWiz
{
    public class ZonaBonus : MonoBehaviour
    {
        public Transform puntoEntradaBonus;
        public Transform puntoDestinoBonus;
        public bool esSalida = false;
        public bool esRedCaidaBonus = false;

        //Tiempo de espera para no teletransportarse dos veces seguidas
        private static float ultimoTeletransporte = -10f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            MovoJugadorTPV jugador = other.GetComponent<MovoJugadorTPV>();
            if (jugador == null) return;

            if (esRedCaidaBonus)
            {
                if (puntoDestinoBonus != null)
                {
                    jugador.Teletransportar(puntoDestinoBonus.position + Vector3.up * 0.5f);
                }
                return;
            }

            if (Time.time - ultimoTeletransporte < 0.6f) return;
            ultimoTeletransporte = Time.time;

            if (esSalida)
            {
                Vector3 destino = puntoEntradaBonus != null ? puntoEntradaBonus.position + Vector3.up * 0.5f : jugador.transform.position;
                jugador.ConfigurarZonaBonus(false, Vector3.zero);
                jugador.Teletransportar(destino);
            }
            else
            {
                Vector3 destino = puntoDestinoBonus != null ? puntoDestinoBonus.position + Vector3.up * 0.5f : jugador.transform.position + Vector3.up * 8f;
                jugador.ConfigurarZonaBonus(true, destino);
                jugador.Teletransportar(destino);
            }
        }
    }
}
