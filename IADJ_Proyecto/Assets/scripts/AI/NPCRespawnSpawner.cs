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

    [Header("Composición del Bando")]
    public int numVigilantes = 2;
    public int numAtaque = 4;
    public int numDefensa = 4;

    [Header("Cantidad Inicial (Calculada)")]
    public int numeroNPCsPorBando = 10;

    [Header("Opciones")]
    private bool spawnAlIniciar = true;
    public bool usarWaypointSiguienteParaRespawn = true;
    public float alturaRespawn = 2.3f;
    public bool irAPuntoMuerteTrasRespawn = true;

    private int indexRespawnRojo;
    private int indexRespawnAzul;
    private int vivosRojo;
    private int vivosAzul;
    private Pathfinding pathfinding;

    private void Awake()
    {
        pathfinding = FindFirstObjectByType<Pathfinding>();
        // Calculamos el total basado en la composición deseada
        numeroNPCsPorBando = numVigilantes + numAtaque + numDefensa;
        RecontarVivosIniciales();
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
        SpawnBandoConComposicion(Bando.Rojo);
        SpawnBandoConComposicion(Bando.Azul);
    }

    private void SpawnBandoConComposicion(Bando bando)
    {
        TipoUnidad[] todosLosTipos = (TipoUnidad[])System.Enum.GetValues(typeof(TipoUnidad));
        int unitTypeIndex = 0;

        // 1. Vigilantes
        for (int i = 0; i < numVigilantes; i++)
        {
            SpawnUnidadConEstado(todosLosTipos[unitTypeIndex], bando, EstadoNPC.Vigilancia);
            unitTypeIndex = (unitTypeIndex + 1) % todosLosTipos.Length;
        }

        // 2. Ataque
        for (int i = 0; i < numAtaque; i++)
        {
            SpawnUnidadConEstado(todosLosTipos[unitTypeIndex], bando, EstadoNPC.Ataque);
            unitTypeIndex = (unitTypeIndex + 1) % todosLosTipos.Length;
        }

        // 3. Defensa
        for (int i = 0; i < numDefensa; i++)
        {
            SpawnUnidadConEstado(todosLosTipos[unitTypeIndex], bando, EstadoNPC.Defensa);
            unitTypeIndex = (unitTypeIndex + 1) % todosLosTipos.Length;
        }
    }

    private void SpawnUnidadConEstado(TipoUnidad tipo, Bando bando, EstadoNPC estadoObj)
    {
        GameObject npc = SpawnNPCEnPosicion(tipo, bando, ObtenerPosicionReaparicion(bando));
        if (npc != null)
        {
            estadoNPC scriptEstado = npc.GetComponent<estadoNPC>();
            if (scriptEstado != null)
            {
                scriptEstado.SetEstado(estadoObj);
            }
        }
    }

    public GameObject SpawnNPCEnPosicion(TipoUnidad tipoUnidad, Bando bando, Vector3 posicion)
    {
        return SpawnNPCEnPosicion(tipoUnidad, bando, posicion, 0);
    }

    private GameObject SpawnNPCEnPosicion(TipoUnidad tipoUnidad, Bando bando, Vector3 posicion, int cupoExtraTemporal)
    {
        if (wayPoints == null)
        {
            Debug.LogError("[NPCRespawnSpawner] No hay referencia a WayPoints.", gameObject);
            return null;
        }

        int vivosDelBando = ObtenerVivos(bando);
        if (vivosDelBando >= numeroNPCsPorBando + cupoExtraTemporal)
        {
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

        IncrementarVivos(bando);

        return npc;
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

        estadoNPC estado = npc.GetComponent<estadoNPC>();
        EstadoNPC estadoActual = estado != null ? estado.GetEstadoActual() : EstadoNPC.Vigilancia;

        RespawnNPCEnPuntoMasCercano(stats.tipoUnidad, stats.miBando, estadoActual, npc.transform.position);
    }

    public void RespawnNPCEnPuntoMasCercano(TipoUnidad tipoUnidad, Bando bando, EstadoNPC estadoActual, Vector3 posicionMuerte)
    {
        if (wayPoints == null)
        {
            Debug.LogError("[NPCRespawnSpawner] No hay referencia a WayPoints.", gameObject);
            return;
        }

        Vector3 posicion = wayPoints.GetWaypointReaparicionMasCercano(bando, posicionMuerte);
        // El NPC muerto puede seguir contado en este frame; cupoExtraTemporal=1 evita bloquear su reemplazo.
        GameObject npcRespawneado = SpawnNPCEnPosicion(tipoUnidad, bando, posicion, 1);
        
        if (npcRespawneado != null)
        {
            estadoNPC scriptEstado = npcRespawneado.GetComponent<estadoNPC>();
            if (scriptEstado != null)
            {
                scriptEstado.SetEstado(estadoActual);
            }
        }

        if (irAPuntoMuerteTrasRespawn)
        {
            EnviarNPCAlPuntoMuerte(npcRespawneado, posicionMuerte);
        }
    }

    public void RegistrarMuerteYRespawn(TipoUnidad tipoUnidad, Bando bando, EstadoNPC estadoActual, Vector3 posicionMuerte)
    {
        DecrementarVivos(bando);
        StartCoroutine(RutinaRespawn(tipoUnidad, bando, estadoActual, posicionMuerte));
    }

    private System.Collections.IEnumerator RutinaRespawn(TipoUnidad tipoUnidad, Bando bando, EstadoNPC estadoActual, Vector3 posicionMuerte)
    {
        // Esperamos 30 segundos antes de reaparecer
        yield return new WaitForSeconds(30f);
        RespawnNPCEnPuntoMasCercano(tipoUnidad, bando, estadoActual, posicionMuerte);
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

        // Add a small random offset so if multiple units spawn at the exact same waypoint, they don't perfectly overlap
        posicion.x += Random.Range(-1.5f, 1.5f);
        posicion.z += Random.Range(-1.5f, 1.5f);

        return posicion;
    }

    private void RecontarVivosIniciales()
    {
        vivosRojo = 0;
        vivosAzul = 0;

        NPCStats[] todos = FindObjectsByType<NPCStats>(FindObjectsSortMode.None);

        foreach (NPCStats stats in todos)
        {
            if (stats == null)
            {
                continue;
            }

            if (stats.miBando == Bando.Rojo) vivosRojo++;
            else if (stats.miBando == Bando.Azul) vivosAzul++;
        }
    }

    private int ObtenerVivos(Bando bando)
    {
        return bando == Bando.Rojo ? vivosRojo : vivosAzul;
    }

    private void IncrementarVivos(Bando bando)
    {
        if (bando == Bando.Rojo) vivosRojo++;
        else if (bando == Bando.Azul) vivosAzul++;
    }

    private void DecrementarVivos(Bando bando)
    {
        if (bando == Bando.Rojo) vivosRojo = Mathf.Max(0, vivosRojo - 1);
        else if (bando == Bando.Azul) vivosAzul = Mathf.Max(0, vivosAzul - 1);
    }

    public int GetVivosRojo() => vivosRojo;
    public int GetVivosAzul() => vivosAzul;
}
