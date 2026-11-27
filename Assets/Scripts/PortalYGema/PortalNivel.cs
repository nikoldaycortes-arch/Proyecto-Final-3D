using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PortalNivel : MonoBehaviour
{
    public string escenaSiguiente = "Nivel2";
    public int coleccionablesNecesarios = 1;
    public GameObject efectoActivo;
    public TMP_Text textoColeccionables;
    public ControladorGameplayUI controladorHUD;

    private int coleccionablesRecogidos = 0;
    private bool cargandoEscena = false;

    private void Start()
    {
        ActualizarEstadoVisual();
    }

    public void RegistrarColeccionable()
    {
        coleccionablesRecogidos++;
        ActualizarEstadoVisual();

        if (controladorHUD != null)
        {
            controladorHUD.MostrarVidasTemporalmente();
        }
    }

    private void ActualizarEstadoVisual()
    {
        if (efectoActivo != null)
        {
            efectoActivo.SetActive(coleccionablesRecogidos >= coleccionablesNecesarios);
        }

        if (textoColeccionables != null)
        {
            textoColeccionables.text = coleccionablesRecogidos + "/" + coleccionablesNecesarios;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cargandoEscena)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (coleccionablesRecogidos < coleccionablesNecesarios)
        {
            return;
        }

        cargandoEscena = true;
        Time.timeScale = 1f;

        if (escenaSiguiente == "MenuPrincipal")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SceneManager.LoadScene(escenaSiguiente);
    }
}