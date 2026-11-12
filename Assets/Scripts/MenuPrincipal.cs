using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Control del juego")]
    public string escenaJuego = "Nivel1";

    private bool juegoPausado = false;

    // BOTON JUGAR
    public void Jugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaJuego);
    }

    // BOTON PAUSAR
    public void Pausar()
    {
        juegoPausado = !juegoPausado;

        if (juegoPausado)
        {
            Time.timeScale = 0f;
            Debug.Log("Juego pausado");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("Juego reanudado");
        }
    }

    // BOTON SALIR
    public void Salir()
    {
        Time.timeScale = 1f;

        Debug.Log("Saliendo del juego");

        Application.Quit();
    }
}
