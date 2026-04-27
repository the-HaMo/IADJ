using UnityEngine;

public class PercepcionNPC : MonoBehaviour
{
    [Header("Detección")]
    public LayerMask capaNPC;
    public float intervaloBusqueda = 0.2f;

    [Header("Coordenadas Hospitales")]
    public Vector3 hospitalRojo;
    public Vector3 hospitalAzul;

    private NPCStats stats;
    private PathFollowing path;
    private Pathfinding pathfinder;
    private AgentNPC agent;
    private NPCPatrol patrol;
    private estadoNPC estado;

    private Transform enemigoActual;
    private Vector3 lastDest;
    private float nextTick;
    private float nextAtaque;

    private static bool mostrarGizmosGlobal = false;

    void Awake()
    {
        stats = GetComponent<NPCStats>();
        path = GetComponent<PathFollowing>();
        pathfinder = FindFirstObjectByType<Pathfinding>();
        agent = GetComponent<AgentNPC>();
        patrol = GetComponent<NPCPatrol>();
        estado = GetComponent<estadoNPC>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) mostrarGizmosGlobal = !mostrarGizmosGlobal;

        // 1. Curación (Prioridad)
        if (stats.NecesitaCuracion())
        {
            enemigoActual = null;
            ActualizarRuta((stats.miBando == Bando.Rojo) ? hospitalRojo : hospitalAzul, 1f);
            return;
        }

        // 2. Búsqueda de enemigos (cada intervalo)
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + intervaloBusqueda;
            enemigoActual = BuscarEnemigoCercano();
        }

        // 3. Comportamiento
        if (enemigoActual != null)
        {
            if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }

            float dist = Vector3.Distance(transform.position, enemigoActual.position);

            if (dist <= stats.rangoAtaque)
            {
                Parar();
                EjecutarAtaque();
            }
            else
            {
                // Perseguir usando la distancia exacta del rangoAtaque
                Vector3 dir = (transform.position - enemigoActual.position).normalized;
                Vector3 destino = enemigoActual.position + dir * stats.rangoAtaque;
                ActualizarRuta(destino, 1f);
            }
        }
        else
        {
            GestionarVigilancia();
        }
    }

    private void EjecutarAtaque()
    {
        if (Time.time >= nextAtaque && enemigoActual != null)
        {
            NPCStats targetStats = enemigoActual.GetComponent<NPCStats>();
            if (targetStats != null)
            {
                targetStats.RecibirDanio(stats.poder);
                nextAtaque = Time.time + stats.velAtaque;
            }
        }
    }

    private void GestionarVigilancia()
    {
        if (patrol != null && !patrol.enabled && estado != null && estado.GetEstadoActual() == EstadoNPC.Vigilancia)
            patrol.enabled = true;
        else if (patrol == null)
            Parar();
    }

    private Transform BuscarEnemigoCercano()
    {
        if (stats.miBando == Bando.Default) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, stats.radioPercepcion, capaNPC);
        Transform mejor = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            NPCStats ts = hit.GetComponent<NPCStats>();
            if (ts != null && ts.miBando != stats.miBando && ts.miBando != Bando.Default)
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist) { minDist = d; mejor = hit.transform; }
            }
        }
        return mejor;
    }

    private void ActualizarRuta(Vector3 destino, float umbral)
    {
        if (Vector3.Distance(lastDest, destino) > umbral)
        {
            lastDest = destino;
            var camino = pathfinder.FindPath(transform.position, destino, stats);
            if (camino != null) path.SetPath(camino);
        }
    }

    private void Parar()
    {
        if (lastDest != Vector3.zero)
        {
            path.FinalizarMovimiento(agent);
            lastDest = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (!mostrarGizmosGlobal || stats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.radioPercepcion);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.rangoAtaque);
    }
}
