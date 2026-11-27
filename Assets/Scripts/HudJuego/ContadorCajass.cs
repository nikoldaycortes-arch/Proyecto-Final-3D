using UnityEngine;
using TMPro;

public class ContadorCajas : MonoBehaviour
{
    public static ContadorCajas instancia;

    public Transform contenedorCajas;
    public TMP_Text textoCajasInicial;
    public TMP_Text textoCajasSumadas;
    public ControladorGameplayUI controladorHUD;

    public string prefijoCaja = "Caja";
    public string[] palabrasExcluidas = { "Metal", "Hierro", "Acero" };

    private int totalCajas = 0;
    private int cajasRotas = 0;

    private void Awake()
    {
        instancia = this;
    }

    private void Start()
    {
        CalcularTotalCajas();
        ActualizarTextos();
    }

    public void CalcularTotalCajas()
    {
        totalCajas = 0;

        if (contenedorCajas != null)
        {
            foreach (Transform hijo in contenedorCajas)
            {
                if (EsCajaDestructible(hijo.name))
                {
                    totalCajas++;
                }
            }
        }
        else
        {
            GameObject[] todos = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject obj in todos)
            {
                if (obj.scene.IsValid() && EsCajaDestructible(obj.name))
                {
                    if (obj.transform.parent == null || !EsCajaDestructible(obj.transform.parent.name))
                    {
                        totalCajas++;
                    }
                }
            }
        }
    }

    private bool EsCajaDestructible(string nombreObjeto)
    {
        if (!nombreObjeto.StartsWith(prefijoCaja))
        {
            return false;
        }

        for (int i = 0; i < palabrasExcluidas.Length; i++)
        {
            if (!string.IsNullOrEmpty(palabrasExcluidas[i]) && nombreObjeto.Contains(palabrasExcluidas[i]))
            {
                return false;
            }
        }

        return true;
    }

    public void SumarCaja()
    {
        cajasRotas++;
        if (totalCajas > 0 && cajasRotas > totalCajas)
        {
            cajasRotas = totalCajas;
        }

        ActualizarTextos();

        if (controladorHUD != null)
        {
            controladorHUD.MostrarVidasTemporalmente();
        }
    }

    private void ActualizarTextos()
    {
        if (textoCajasInicial != null)
        {
            textoCajasInicial.text = totalCajas.ToString();
        }

        if (textoCajasSumadas != null)
        {
            textoCajasSumadas.text = cajasRotas.ToString();
        }
    }
}