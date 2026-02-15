using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la selección de NPCs mediante ratón y teclado.
/// Los NPCs seleccionados se mantienen hasta que el usuario los descarte.
/// </summary>
public class SeleccionarObjetivos : MonoBehaviour
{
    // Lista de NPCs seleccionados
    private List<GameObject> listNPCs = new List<GameObject>();

    // Material para resaltar NPCs seleccionados
    [SerializeField] private Material materialSeleccionado;
    [SerializeField] private Material materialOriginal;

    // Referencia a la capa de NPCs
    [SerializeField] private LayerMask npcLayer;

    void Update()
    {
        // Añadir NPC a selección con Shift + Clic (verificar primero)
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {
            SeleccionarNPC(true); // true = añadir a selección existente
        }
        // Seleccionar NPC con clic izquierdo (reemplazar selección)
        else if (Input.GetMouseButtonDown(0))
        {
            SeleccionarNPC(false); // false = no añadir, reemplazar selección
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

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, npcLayer))
        {
            GameObject npcClickeado = hit.collider.gameObject;

            // Verificar que tiene componente AgentNPC
            if (npcClickeado.GetComponent<AgentNPC>() != null)
            {
                // Si no es añadir, limpiar selección previa
                if (!añadir)
                {
                    DeseleccionarTodos();
                }

                // Si ya está seleccionado, deseleccionarlo
                if (listNPCs.Contains(npcClickeado))
                {
                    DeseleccionarNPC(npcClickeado);
                }
                else
                {
                    // Añadir a la selección
                    listNPCs.Add(npcClickeado);
                    ResaltarNPC(npcClickeado, true);
                    Debug.Log($"NPC seleccionado: {npcClickeado.name}. Total: {listNPCs.Count}");
                }
            }
        }
    }

    private void DeseleccionarNPC(GameObject npc)
    {
        if (listNPCs.Contains(npc))
        {
            listNPCs.Remove(npc);
            ResaltarNPC(npc, false);
            Debug.Log($"NPC deseleccionado: {npc.name}. Total: {listNPCs.Count}");
        }
    }

    public void DeseleccionarTodos()
    {
        foreach (GameObject npc in listNPCs)
        {
            ResaltarNPC(npc, false);
        }
        listNPCs.Clear();
        Debug.Log("Todos los NPCs deseleccionados");
    }

    private void ResaltarNPC(GameObject npc, bool seleccionar)
    {
        Renderer renderer = npc.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (seleccionar && materialSeleccionado != null)
            {
                renderer.material = materialSeleccionado;
            }
            else if (materialOriginal != null)
            {
                renderer.material = materialOriginal;
            }
        }
    }

    public List<GameObject> getListNPCs()
    {
        return listNPCs;
    }

    public int GetNumSeleccionados()
    {
        return listNPCs.Count;
    }
}
