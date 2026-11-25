using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class ControladorGameplayUI : MonoBehaviour
{
    public GameObject panelPausa;
    public CanvasGroup grupoVidasHUD;
    public string nombreEscenaMenu = "MenuPrincipal";

    public AudioMixer mixerPrincipal;
    public Light luzDireccional;

    public float tiempoVisibleVidas = 5f;
    public float duracionDesvanecimiento = 1.5f;

    private bool estaPausado = false;
    private Coroutine rutinaVidas;

    void Start()
    {
        Time.timeScale = 1f;

        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }

        if (grupoVidasHUD != null)
        {
            grupoVidasHUD.alpha = 0f;
        }

        AplicarConfiguracionGuardada();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (estaPausado)
            {
                ContinuarJuego();
            }
            else
            {
                PausarJuego();
            }
        }

        if (Input.GetKeyDown(KeyCode.L) && !estaPausado)
        {
            MostrarVidasTemporalmente();
        }
    }

    public void PausarJuego()
    {
        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
        }

        Time.timeScale = 0f;
        estaPausado = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ContinuarJuego()
    {
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }

        Time.timeScale = 1f;
        estaPausado = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    public void MostrarVidasTemporalmente()
    {
        if (grupoVidasHUD == null)
        {
            return;
        }

        if (rutinaVidas != null)
        {
            StopCoroutine(rutinaVidas);
        }

        rutinaVidas = StartCoroutine(RutinaDesvanecerVidas());
    }

    private IEnumerator RutinaDesvanecerVidas()
    {
        grupoVidasHUD.alpha = 1f;

        yield return new WaitForSeconds(tiempoVisibleVidas);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionDesvanecimiento)
        {
            tiempoTranscurrido += Time.deltaTime;
            grupoVidasHUD.alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / duracionDesvanecimiento);
            yield return null;
        }

        grupoVidasHUD.alpha = 0f;
        rutinaVidas = null;
    }

    private void AplicarConfiguracionGuardada()
    {
        float musicaGuardada = PlayerPrefs.GetFloat("VolumenMusica", 1f);
        float sfxGuardado = PlayerPrefs.GetFloat("VolumenSFX", 1f);
        float luzGuardada = PlayerPrefs.GetFloat("NivelIluminacion", 1f);

        if (mixerPrincipal != null)
        {
            mixerPrincipal.SetFloat("VolumenMusica", Mathf.Log10(musicaGuardada) * 20f);
            mixerPrincipal.SetFloat("VolumenSFX", Mathf.Log10(sfxGuardado) * 20f);
        }

        if (luzDireccional != null)
        {
            luzDireccional.intensity = luzGuardada;
        }

        RenderSettings.ambientIntensity = luzGuardada;
    }
}