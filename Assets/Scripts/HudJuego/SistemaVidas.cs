using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SistemaVidas : MonoBehaviour
{
    public static SistemaVidas instancia;

    public int vidasIniciales = 4;
    public TMP_Text textoVidaInicial;
    public TMP_Text textoVidaRestante;
    public ControladorGameplayUI controladorHUD;
    public float retrasoReinicio = 2f;

    private int vidasRestantes;
    private bool reiniciando = false;

    public bool SinVidas => vidasRestantes <= 0;

    private void Awake()
    {
        instancia = this;
    }

    private void Start()
    {
        vidasRestantes = vidasIniciales;
        ActualizarTextos();
    }

    public void DescontarVida(int cantidad = 1)
    {
        if (reiniciando)
        {
            return;
        }

        vidasRestantes = Mathf.Max(0, vidasRestantes - cantidad);
        ActualizarTextos();

        if (controladorHUD != null)
        {
            controladorHUD.MostrarVidasTemporalmente();
        }

        if (vidasRestantes <= 0)
        {
            StartCoroutine(ReiniciarNivel());
        }
    }

    public void SumarVida(int cantidad = 1)
    {
        vidasRestantes = Mathf.Min(vidasIniciales, vidasRestantes + cantidad);
        ActualizarTextos();

        if (controladorHUD != null)
        {
            controladorHUD.MostrarVidasTemporalmente();
        }
    }

    private void ActualizarTextos()
    {
        if (textoVidaInicial != null)
        {
            textoVidaInicial.text = vidasIniciales.ToString();
        }

        if (textoVidaRestante != null)
        {
            textoVidaRestante.text = vidasRestantes.ToString();
        }
    }

    private IEnumerator ReiniciarNivel()
    {
        reiniciando = true;
        yield return new WaitForSeconds(retrasoReinicio);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}