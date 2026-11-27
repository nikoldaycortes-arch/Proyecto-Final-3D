using UnityEngine;
using UnityEditor;

public class ReemplazadorWumpas : EditorWindow
{
    private GameObject miPrefabWumpa;
    private string textoEnNombre = "Fruta_Wumpa";
    private float escalaUniforme = 1f;

    [MenuItem("Herramientas/Reemplazar Wumpas en Escena")]
    public static void MostrarVentana()
    {
        GetWindow<ReemplazadorWumpas>("Reemplazar Wumpas");
    }

    private void OnGUI()
    {
        miPrefabWumpa = (GameObject)EditorGUILayout.ObjectField("Tu Prefab Wumpa (Nivel 1)", miPrefabWumpa, typeof(GameObject), false);
        textoEnNombre = EditorGUILayout.TextField("Nombre a buscar en escena", textoEnNombre);
        escalaUniforme = EditorGUILayout.FloatField("Escala deseada", escalaUniforme);

        if (GUILayout.Button("Reemplazar Todas") && miPrefabWumpa != null)
        {
            GameObject[] todos = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int contador = 0;

            foreach (GameObject obj in todos)
            {
                if (obj == null || !obj.scene.IsValid())
                {
                    continue;
                }

                if (!obj.name.Contains(textoEnNombre))
                {
                    continue;
                }

                if (obj.transform.parent != null && obj.transform.parent.name.Contains(textoEnNombre))
                {
                    continue;
                }

                Transform padre = obj.transform.parent;
                Vector3 pos = obj.transform.position;
                Quaternion rot = obj.transform.rotation;
                bool estabaActivo = obj.activeSelf;

                GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(miPrefabWumpa, padre);
                nueva.transform.position = pos;
                nueva.transform.rotation = rot;
                nueva.transform.localScale = Vector3.one * escalaUniforme;
                nueva.SetActive(estabaActivo);

                Undo.RegisterCreatedObjectUndo(nueva, "Reemplazar Wumpa");
                Undo.DestroyObjectImmediate(obj);
                contador++;
            }

            Debug.Log("Wumpas reemplazadas: " + contador);
        }
    }
}

