using System.Collections.Generic;
using UnityEngine;
public class WayPoints : MonoBehaviour
{
    [Header("Puntos de Reaparicion")]
    public List<Transform> spawnRojo;
    public List<Transform> spawnAzul;

    [Header("Otros Puntos")]
    public Transform hospitalRojo;
    public Transform hospitalAzul;
    public Transform spawnCastilloRojo;
    public Transform spawnCastilloAzul;

    [Header("Castillos (objetivo de victoria)")]
    [Tooltip("Castillo Rojo: si lo destruye Azul, gana Azul. Asignar el mismo Transform que en CastilloDerrumbado.")]
    public Transform castilloRojo;
    [Tooltip("Castillo Azul: si lo destruye Rojo, gana Rojo. Asignar el mismo Transform que en CastilloDerrumbado.")]
    public Transform castilloAzul;

    // Getters basicos para el Spawner
    public Vector3 GetRandomReaparicion(Bando bando)
    {
        List<Transform> lista = (bando == Bando.Rojo) ? spawnRojo : spawnAzul;
        lista.RemoveAll(t => t == null);

        if (lista.Count > 0)
        {
            Transform t = lista[Random.Range(0, lista.Count)];
            return t.position;
        }
        return GetSpawnCastillo(bando);
    }

    public Vector3 GetWaypointReaparicion(Bando bando, int index)
    {
        List<Transform> lista = (bando == Bando.Rojo) ? spawnRojo : spawnAzul;
        lista.RemoveAll(t => t == null);

        if (lista.Count > 0)
        {
            Transform t = lista[index % lista.Count];
            return t.position;
        }
        return GetSpawnCastillo(bando);
    }

    public Vector3 GetWaypointReaparicionMasCercano(Bando bando, Vector3 posMuerte)
    {
        List<Transform> lista = (bando == Bando.Rojo) ? spawnRojo : spawnAzul;
        lista.RemoveAll(t => t == null);

        if (lista.Count == 0) return GetSpawnCastillo(bando);

        Transform mejor = lista[0];
        float minDist = Vector3.Distance(posMuerte, mejor.position);

        foreach (Transform t in lista)
        {
            float d = Vector3.Distance(posMuerte, t.position);
            if (d < minDist)
            {
                minDist = d;
                mejor = t;
            }
        }
        return mejor.position;
    }

    // Getters para Hospital y Base
    public Vector3 GetCuracion(Bando bando)
    {
        Transform h = (bando == Bando.Rojo) ? hospitalRojo : hospitalAzul;
        return (h != null) ? h.position : GetSpawnCastillo(bando);
    }

    public Vector3 GetSpawnCastillo(Bando bando)
    {
        Transform b = (bando == Bando.Rojo) ? spawnCastilloRojo : spawnCastilloAzul;
        if (b != null) return b.position;

        // Fallback: Si no hay spawn de castillo asignado, intentamos usar el transform de victoria
        Transform c = (bando == Bando.Rojo) ? castilloRojo : castilloAzul;
        return (c != null) ? c.position : Vector3.zero;
    }

    /// <summary>
    /// Devuelve la posicion del castillo enemigo (objetivo de victoria) para
    /// "miBando". Si el castillo enemigo ha sido destruido o no esta asignado,
    /// devuelve la base enemiga como fallback.
    /// </summary>
    public Vector3 GetCastilloEnemigo(Bando miBando)
    {
        Transform c = (miBando == Bando.Rojo) ? castilloAzul : castilloRojo;
        if (c != null) return c.position;
        // fallback: base enemiga
        Bando enemigo = (miBando == Bando.Rojo) ? Bando.Azul : Bando.Rojo;
        return GetSpawnCastillo(enemigo);
    }

    /// <summary>
    /// Devuelve la posicion del castillo PROPIO (lo que hay que defender).
    /// </summary>
    public Vector3 GetCastilloPropio(Bando miBando)
    {
        Transform c = (miBando == Bando.Rojo) ? castilloRojo : castilloAzul;
        if (c != null) return c.position;
        return GetSpawnCastillo(miBando);
    }

    /// <summary>
    /// True si el castillo enemigo sigue en pie. False si ya cayo.
    /// </summary>
    public bool CastilloEnemigoVivo(Bando miBando)
    {
        Transform c = (miBando == Bando.Rojo) ? castilloAzul : castilloRojo;
        return c != null;
    }

    /// <summary>
    /// Devuelve la posición del objetivo (Base o Torre/Spawn) enemigo más cercano a miPos.
    /// </summary>
    public Vector3 GetObjetivoMasCercano(Bando bandoEnemigo, Vector3 miPos)
    {
        List<Transform> torres = (bandoEnemigo == Bando.Rojo) ? spawnRojo : spawnAzul;
        
        // El objetivo final es el Castillo (si existe), si no, la Base.
        Transform objetivoFinal = (bandoEnemigo == Bando.Rojo) ? castilloRojo : castilloAzul;
        if (objetivoFinal == null) 
            objetivoFinal = (bandoEnemigo == Bando.Rojo) ? spawnCastilloRojo : spawnCastilloAzul;

        Transform mejor = objetivoFinal;
        float minDist = (objetivoFinal != null) ? Vector3.Distance(miPos, objetivoFinal.position) : float.MaxValue;

        foreach (Transform t in torres)
        {
            if (t == null) continue;
            float d = Vector3.Distance(miPos, t.position);
            if (d < minDist)
            {
                minDist = d;
                mejor = t;
            }
        }
        return (mejor != null) ? mejor.position : transform.position;
    }

    public void DesactivarPuntoSpawn(Bando bando, GameObject puntoObj)
    {
        List<Transform> lista = (bando == Bando.Rojo) ? spawnRojo : spawnAzul;
        lista.RemoveAll(t => t == null || Vector3.Distance(t.position, puntoObj.transform.position) < 3f);
    }

    private void OnDrawGizmos()
    {
        if (!EstadoTacticoGlobal.DebugActivo) return;

        // --- Spawns Rojo (esferas + líneas) ---
        DibujarListaPuntos(spawnRojo, Color.red, "S");
        // --- Spawns Azul ---
        DibujarListaPuntos(spawnAzul, Color.blue, "S");

        // --- Hospitales ---
        DibujarPunto(hospitalRojo, Color.red, 0.8f);
        DibujarPunto(hospitalAzul, Color.blue, 0.8f);

        // --- Bases ---
        DibujarPunto(spawnCastilloRojo, new Color(1f, 0.4f, 0f), 1.2f);
        DibujarPunto(spawnCastilloAzul, new Color(0f, 0.4f, 1f), 1.2f);

        // Líneas: Spawn Rojo -> Hospital Rojo -> Spawn Castillo Rojo
        DibujarCadena(spawnRojo, hospitalRojo, spawnCastilloRojo, Color.red);
        // Líneas: Spawn Azul -> Hospital Azul -> Spawn Castillo Azul
        DibujarCadena(spawnAzul, hospitalAzul, spawnCastilloAzul, Color.blue);
    }

    private void DibujarListaPuntos(List<Transform> lista, Color color, string prefijo)
    {
        if (lista == null) return;
        Gizmos.color = color;
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i] == null) continue;
            Gizmos.DrawWireSphere(lista[i].position, 0.5f);
            // Línea entre spawns consecutivos
            if (i + 1 < lista.Count && lista[i + 1] != null)
                Gizmos.DrawLine(lista[i].position, lista[i + 1].position);
        }
    }

    private void DibujarPunto(Transform t, Color color, float radio)
    {
        if (t == null) return;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(t.position, radio);
    }

    private void DibujarCadena(List<Transform> spawns, Transform hospital, Transform baseT, Color color)
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
        if (hospital != null && baseT != null)
            Gizmos.DrawLine(hospital.position, baseT.position);

        if (spawns == null) return;
        foreach (var s in spawns)
        {
            if (s == null) continue;
            if (hospital != null) Gizmos.DrawLine(s.position, hospital.position);
        }
    }
}
