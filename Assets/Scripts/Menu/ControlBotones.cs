using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorMenuPrincipal : MonoBehaviour
{
    public GameObject panelOpciones;
    public string nombreEscenaJuego = "Nivel1";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false);
        }
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true);
        }
    }

    public void SalirDelJuego()
    {
        Application.Quit();
    }
}