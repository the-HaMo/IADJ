using System.Collections.Generic;
using UnityEngine;

public class PercepcionNPC : MonoBehaviour
{
    public LayerMask capaNPC;
    
    [Header("Coordenadas Hospitales")]
    public Vector3 hospitalRojo;
    public Vector3 hospitalAzul;
    
    public float intervaloBusqueda = 0.2f;

    private NPCStats stats;
    private PathFollowing path;
    private Pathfinding pathfinder;
    private Vector3 lastDest;
    private float nextTick;
    private float nextAtaque;
    private Transform enemigoActual;
    private AgentNPC agent;
    private NPCPatrol patrol;
    private estadoNPC estado;

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

        if (stats.NecesitaCuracion())
        {
            enemigoActual = null;
            LogicaCuracion();
        }
        else if (enemigoActual != null)
        {
            LogicaAtaque();
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + intervaloBusqueda;

        if (!stats.NecesitaCuracion())
        {
            LogicaBusquedaYCombate();
        }
    }

    private void LogicaCuracion()
    {
        Vector3 destino;
        if (stats.miBando == Bando.Rojo)
        {
            destino = hospitalRojo;
        }
        else
        {
            destino = hospitalAzul;
        }
        ActualizarRuta(destino, 1f);
    }

    private void LogicaAtaque()
    {
        float dist = Vector3.Distance(transform.position, enemigoActual.position);
        if (dist <= stats.rangoAtaque)
        {
            Parar();
            if (Time.time >= nextAtaque)
            {
                NPCStats targetStats = enemigoActual.GetComponent<NPCStats>();
                if (targetStats != null)
                {
                    targetStats.RecibirDanio(stats.poder);
                    nextAtaque = Time.time + stats.velAtaque;
                }
            }
        }
    }

    private void LogicaBusquedaYCombate()
    {
        enemigoActual = BuscarEnemigoCercano();

        if (enemigoActual != null)
        {
            // Hay enemigo: pausamos la patrulla si estaba activa
            if (patrol != null)
            {
                if (patrol.enabled == true)
                {
                    patrol.enabled = false;
                    patrol.DetenerPatrulla();
                }
            }

            // Perseguimos al enemigo si esta fuera de rango
            float dist = Vector3.Distance(transform.position, enemigoActual.position);
            if (dist > stats.rangoAtaque)
            {
                ActualizarRuta(enemigoActual.position, 2f);
            }
        }
        else
        {
            // Sin enemigo: reanudamos la patrulla solo si el NPC esta en estado Vigilancia
            if (patrol != null)
            {
                if (patrol.enabled == false)
                {
                    if (estado != null)
                    {
                        if (estado.GetEstadoActual() == EstadoNPC.Vigilancia)
                        {
                            patrol.enabled = true;
                        }
                    }
                }
            }
            else
            {
                Parar();
            }
        }
    }

    private Transform BuscarEnemigoCercano()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.radioPercepcion, capaNPC);
        Transform mejorTarget = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            NPCStats targetStats = hit.GetComponent<NPCStats>();
            if (targetStats && targetStats.miBando != stats.miBando)
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < minDist) { minDist = d; mejorTarget = hit.transform; }
            }
        }
        return mejorTarget;
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
        if (mostrarGizmosGlobal && stats != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.radioPercepcion);
        }

        if (stats != null && stats.NecesitaCuracion())
        {
            Gizmos.color = Color.green;
            Vector3 posIcono = transform.position + Vector3.up * 2.5f;
            Gizmos.DrawWireSphere(posIcono, 0.3f);
            Gizmos.DrawLine(posIcono + Vector3.left * 0.2f, posIcono + Vector3.right * 0.2f);
            Gizmos.DrawLine(posIcono + Vector3.down * 0.2f, posIcono + Vector3.up * 0.2f);
        }
    }
}
