using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la selección de NPCs mediante ratón y teclado.
/// Los NPCs seleccionados se mantienen hasta que el usuario los descarte.
/// Al seleccionar un NPC se muestra una pirámide invertida encima de él.
/// </summary>
public class SeleccionarObjetivos : MonoBehaviour
{
    // Lista de NPCs seleccionados
    private List<GameObject> listNPCs = new List<GameObject>();

    // Pirámides activas por NPC
    private Dictionary<GameObject, GameObject> piramides = new Dictionary<GameObject, GameObject>();

    // Configuración visual de la pirámide
    [SerializeField] private float alturaSobreNPC = 2.0f;
    [SerializeField] private float tamañoBase = 0.4f;
    [SerializeField] private Color colorPiramide = Color.yellow;

    void Update()
    {
        // Añadir NPC a selección con Shift + Clic
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {
            SeleccionarNPC(true);
        }
        // Seleccionar NPC con clic izquierdo (reemplaza selección)
        else if (Input.GetMouseButtonDown(0))
        {
            SeleccionarNPC(false);
        }

        // Deseleccionar todos con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeseleccionarTodos();
        }
    }

    private void SeleccionarNPC(bool añadir)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject npcClickeado = hit.collider.gameObject;

            if (npcClickeado.GetComponent<AgentNPC>() != null)
            {
                if (!añadir)
                    DeseleccionarTodos();

                if (listNPCs.Contains(npcClickeado))
                {
                    DeseleccionarNPC(npcClickeado);
                }
                else
                {
                    listNPCs.Add(npcClickeado);
                    MostrarPiramide(npcClickeado, true);
                    Debug.Log($"NPC seleccionado: {npcClickeado.name}. Total: {listNPCs.Count}");
                }
            }
        }
    }

    public void DeseleccionarNPC(GameObject npc)
    {
        if (listNPCs.Contains(npc))
        {
            listNPCs.Remove(npc);
            MostrarPiramide(npc, false);
            Debug.Log($"NPC deseleccionado: {npc.name}. Total: {listNPCs.Count}");
        }
    }

    public void DeseleccionarTodos()
    {
        foreach (GameObject npc in listNPCs)
            MostrarPiramide(npc, false);
        listNPCs.Clear();
        Debug.Log("Todos los NPCs deseleccionados");
    }

    private void MostrarPiramide(GameObject npc, bool mostrar)
    {
        if (mostrar)
        {
            if (piramides.ContainsKey(npc)) return;

            GameObject piramide = CrearPiramideInvertida();
            piramide.transform.SetParent(npc.transform, false);
            piramide.transform.localPosition = new Vector3(0, alturaSobreNPC, 0);
            piramides[npc] = piramide;
        }
        else
        {
            if (piramides.TryGetValue(npc, out GameObject piramide))
            {
                Destroy(piramide);
                piramides.Remove(npc);
            }
        }
    }

    private GameObject CrearPiramideInvertida()
    {
        GameObject obj = new GameObject("PiramideSeleccion");
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        mf.mesh = GenerarMeshPiramideInvertida(tamañoBase);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = colorPiramide;
        mr.material = mat;

        return obj;
    }

    /// <summary>
    /// Genera un mesh de pirámide invertida: base cuadrada arriba, ápice apuntando hacia abajo.
    /// </summary>
    private Mesh GenerarMeshPiramideInvertida(float size)
    {
        Mesh mesh = new Mesh();

        float h = size * 1.5f; // altura de la pirámide
        float s = size * 0.5f; // mitad del lado de la base

        // Base en y=0 (local), ápice en y=-h
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-s, 0,  s),  // 0: frente-izq
            new Vector3( s, 0,  s),  // 1: frente-der
            new Vector3( s, 0, -s),  // 2: atrás-der
            new Vector3(-s, 0, -s),  // 3: atrás-izq
            new Vector3( 0, -h,  0), // 4: ápice (abajo)
        };

        int[] triangles = new int[]
        {
            // Base (cara superior)
            0, 2, 1,
            0, 3, 2,
            // Cara frontal
            0, 1, 4,
            // Cara derecha
            1, 2, 4,
            // Cara trasera
            2, 3, 4,
            // Cara izquierda
            3, 0, 4,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    public List<GameObject> getListNPCs() => listNPCs;
    public int GetNumSeleccionados() => listNPCs.Count;
}
