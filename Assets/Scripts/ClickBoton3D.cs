
using UnityEngine;
using System.Collections;

public class ClickBoton3D : MonoBehaviour
{
    public MenuPrincipal menu;
    public AnimacionBoton animacion;
    public string accion;

    private bool presionado = false;

    private void OnMouseDown()
    {
        if (presionado) return;

        StartCoroutine(EjecutarAccion());
    }

    IEnumerator EjecutarAccion()
    {
        presionado = true;

        if (animacion != null)
        {
            yield return StartCoroutine(animacion.Brinquito());
        }

        switch (accion)
        {
            case "JUGAR":
                menu.Jugar();
                break;

            case "PAUSAR":
                menu.Pausar();
                break;

            case "SALIR":
                menu.Salir();
                break;
        }

        presionado = false;
    }
}
