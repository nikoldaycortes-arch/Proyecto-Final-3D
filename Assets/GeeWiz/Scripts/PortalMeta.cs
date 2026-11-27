using UnityEngine;

namespace GeeWiz
{
    public class PortalMeta : MonoBehaviour
    {
        public TextMesh textoProgreso3D;
        public GameObject vfxTeletransporte;

        private bool activado = false;

        private void Update()
        {
            if (textoProgreso3D != null && GestorJuego.Instancia != null)
            {
                string estadoGema = GestorJuego.Instancia.gemaObtenida ? "OK" : "Pendiente";
                textoProgreso3D.text = $"Cajas: {GestorJuego.Instancia.cajasRotas}/{GestorJuego.Instancia.cajasTotales}\nGema: {estadoGema}";
            }
        }

        //Al tocar el portal se pausa el juego y sale el panel de nivel completado
        private void OnTriggerEnter(Collider other)
        {
            if (activado || !other.CompareTag("Player")) return;
            activado = true;

            if (vfxTeletransporte != null)
            {
                Instantiate(vfxTeletransporte, transform.position, Quaternion.identity);
            }

            if (GestorJuego.Instancia != null)
            {
                GestorJuego.Instancia.DetenerRelojMeta();
                GestorJuego.Instancia.CompletarNivel();
            }
        }
    }
}
