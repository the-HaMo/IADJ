using UnityEngine;
using System.Collections.Generic;

public class NPCRespawnSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public WayPoints wayPoints;
    public GameObject prefabCaballero;
    public GameObject prefabArquero;
    public GameObject prefabLancero;
    public GameObject prefabTanque;
    public GameObject prefabExplorador;

    [Header("Tipo por Defecto (si llamas SpawnNPC por bando)")]
    public TipoUnidad tipoUnidadDefault = TipoUnidad.Caballero;

    [Header("Cantidad Inicial")]
    public int numeroNPCsRojo = 3;
    public int numeroNPCsAzul = 3;

    [Header("Opciones")]
    public bool spawnAlIniciar = false;
    public bool usarWaypointSiguienteParaRespawn = true;
    public float alturaRespawn = 2.3f;
    public bool irAPuntoMuerteTrasRespawn = true;

    private int indexRespawnRojo;
    private int indexRespawnAzul;
    private Pathfinding pathfinding;

    private void Awake()
    {
        pathfinding = FindFirstObjectByType<Pathfinding>();
    }

    private void Start()
    {
        if (spawnAlIniciar)
        {
            SpawnInicial();
        }
    }

    public void SpawnInicial()
    {
        SpawnBando(Bando.Rojo, numeroNPCsRojo);
        SpawnBando(Bando.Azul, numeroNPCsAzul);
    }

    public GameObject SpawnNPC(Bando bando)
    {
        return SpawnNPCEnPosicion(tipoUnidadDefault, bando, ObtenerPosicionReaparicion(bando));
    }

    public GameObject SpawnNPCEnPosicion(TipoUnidad tipoUnidad, Bando bando, Vector3 posicion)
    {
        if (wayPoints == null)
        {
            Debug.LogError("[NPCRespawnSpawner] No hay referencia a WayPoints.", gameObject);
            return null;
        }

        posicion.y = alturaRespawn;

        GameObject prefab = ObtenerPrefabPorTipo(tipoUnidad);
        if (prefab == null)
        {
            Debug.LogError($"[NPCRespawnSpawner] No hay prefab asignado para el tipo {tipoUnidad}.", gameObject);
            return null;
        }

        GameObject npc = Instantiate(prefab, posicion, Quaternion.identity);
        NPCStats stats = npc.GetComponent<NPCStats>();
        if (stats != null)
        {
            stats.tipoUnidad = tipoUnidad;
            stats.miBando = bando;
            stats.AplicarMaterial();
        }

        return npc;
    }

    public GameObject SpawnNPCEnPosicion(NPCStats plantilla, Vector3 posicion)
    {
        if (plantilla == null)
        {
            return null;
        }

        return SpawnNPCEnPosicion(plantilla.tipoUnidad, plantilla.miBando, posicion);
    }

    public void SpawnBando(Bando bando, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            SpawnNPC(bando);
        }
    }

    public void RespawnNPC(GameObject npc)
    {
        if (npc == null)
        {
            return;
        }

        NPCStats stats = npc.GetComponent<NPCStats>();
        if (stats == null)
        {
            return;
        }

        RespawnNPCEnPuntoMasCercano(stats.tipoUnidad, stats.miBando, npc.transform.position);
    }

    public void RespawnNPCEnPuntoMasCercano(NPCStats plantilla, Vector3 posicionMuerte)
    {
        if (plantilla == null)
        {
            return;
        }

        RespawnNPCEnPuntoMasCercano(plantilla.tipoUnidad, plantilla.miBando, posicionMuerte);
    }

    public void RespawnNPCEnPuntoMasCercano(TipoUnidad tipoUnidad, Bando bando, Vector3 posicionMuerte)
    {
        if (wayPoints == null)
        {
            Debug.LogError("[NPCRespawnSpawner] No hay referencia a WayPoints.", gameObject);
            return;
        }

        Vector3 posicion = wayPoints.GetWaypointReaparicionMasCercano(bando, posicionMuerte);
        GameObject npcRespawneado = SpawnNPCEnPosicion(tipoUnidad, bando, posicion);

        if (irAPuntoMuerteTrasRespawn)
        {
            EnviarNPCAlPuntoMuerte(npcRespawneado, posicionMuerte);
        }
    }

    private void EnviarNPCAlPuntoMuerte(GameObject npc, Vector3 posicionMuerte)
    {
        if (npc == null)
        {
            return;
        }

        if (pathfinding == null)
        {
            pathfinding = FindFirstObjectByType<Pathfinding>();
        }

        if (pathfinding == null)
        {
            Debug.LogWarning("[NPCRespawnSpawner] No se encontró Pathfinding para enviar al punto de muerte.", gameObject);
            return;
        }

        PathFollowing path = npc.GetComponent<PathFollowing>();
        NPCStats stats = npc.GetComponent<NPCStats>();
        if (path == null || stats == null)
        {
            return;
        }

        Vector3 destino = posicionMuerte;
        destino.y = 0f;

        List<Node> camino = pathfinding.FindPath(npc.transform.position, destino, stats);
        if (camino != null && camino.Count > 0)
        {
            path.SetPath(camino);
        }
    }

    private GameObject ObtenerPrefabPorTipo(TipoUnidad tipoUnidad)
    {
        return tipoUnidad switch
        {
            TipoUnidad.Caballero => prefabCaballero,
            TipoUnidad.Arquero => prefabArquero,
            TipoUnidad.Lancero => prefabLancero,
            TipoUnidad.Tanque => prefabTanque,
            TipoUnidad.Explorador => prefabExplorador,
            _ => null,
        };
    }

    private Vector3 ObtenerPosicionReaparicion(Bando bando)
    {
        if (wayPoints == null)
        {
            return Vector3.zero;
        }

        Vector3 posicion;

        if (usarWaypointSiguienteParaRespawn)
        {
            if (bando == Bando.Rojo)
            {
                posicion = wayPoints.GetWaypointReaparicion(bando, indexRespawnRojo);
                indexRespawnRojo++;
            }
            else
            {
                posicion = wayPoints.GetWaypointReaparicion(bando, indexRespawnAzul);
                indexRespawnAzul++;
            }
        }
        else
        {
            posicion = wayPoints.GetWaypointReaparicion(bando, 0);
        }

        return posicion;
    }
}
