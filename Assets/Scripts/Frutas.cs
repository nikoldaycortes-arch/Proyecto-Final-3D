using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Fruta : MonoBehaviour
{
    public TMP_Text textoContador;
    public float velocidadGiro = 120f;
    public int valor = 1;

    private static int totalFrutas = 0;
    private static bool escenaSuscrita = false;

    private void Awake()
    {
        if (!escenaSuscrita)
        {
            SceneManager.sceneLoaded += ReiniciarContador;
            escenaSuscrita = true;
        }
    }

    private static void ReiniciarContador(Scene escena, LoadSceneMode modo)
    {
        totalFrutas = 0;
    }

    private void Start()
    {
        if (textoContador == null)
        {
            GameObject objContador = GameObject.Find("ContadorWumpa");
            if (objContador != null)
            {
                textoContador = objContador.GetComponentInChildren<TMP_Text>();
            }
        }

        if (textoContador != null)
        {
            textoContador.text = totalFrutas.ToString();
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            totalFrutas += valor;
            if (textoContador != null)
            {
                textoContador.text = totalFrutas.ToString();
            }
            Destroy(gameObject);
        }
    }
}