using System.Collections.Generic;
using UnityEngine;

public class WayPoints : MonoBehaviour
{
    [Header("Waypoints Reaparicion Rojo")]
    public List<Vector3> waypointReaparicionRojo = new List<Vector3>();
    
    [Header("Waypoints Reaparicion Azul")]
    public List<Vector3> waypointReaparicionAzul = new List<Vector3>();
    
    [Header("Waypoints Curacion Rojo")]
    public List<Vector3> waypointCuracionRojo = new List<Vector3>();
    
    [Header("Waypoints Curacion Azul")]
    public List<Vector3> waypointCuracionAzul = new List<Vector3>();
    
    [Header("Waypoints Base Rojo")]
    public List<Vector3> waypointBaseRojo = new List<Vector3>();
    
    [Header("Waypoints Base Azul")]
    public List<Vector3> waypointBaseAzul = new List<Vector3>();

    [Header("Waypoints Tactico Rojo")]
    public List<Vector3> waypointTacticoRojo = new List<Vector3>();

    [Header("Waypoints Tactico Azul")]
    public List<Vector3> waypointTacticoAzul = new List<Vector3>();

    [Header("Debug")]
    [SerializeField] private bool debugWaypoints = true;
    [SerializeField] private float radioGizmo = 1f;

    private void Awake()
    {
        InitializeWaypoints();
    }

    private void InitializeWaypoints()
    {
        waypointReaparicionRojo.Clear();
        waypointReaparicionAzul.Clear();
        waypointCuracionRojo.Clear();
        waypointCuracionAzul.Clear();
        waypointBaseRojo.Clear();
        waypointBaseAzul.Clear();
        waypointTacticoRojo.Clear();
        waypointTacticoAzul.Clear();

        // Reaparicion Rojo
        waypointReaparicionRojo.Add(new Vector3(92.4f, 0f, -47.5f));
        waypointReaparicionRojo.Add(new Vector3(-6.67f, 0f, -95.8f));
        waypointReaparicionRojo.Add(new Vector3(-96.6f, 0f, -116.6f));

        // Reaparicion Azul
        waypointReaparicionAzul.Add(new Vector3(80.3f, 0f, 87.1f));
        waypointReaparicionAzul.Add(new Vector3(-6.87f, 0f, 66.5f));
        waypointReaparicionAzul.Add(new Vector3(-103f, 0f, 27.3f));

        // Curacion Rojo y Azul
        waypointCuracionRojo.Add(new Vector3(20.2f, 0f, -138f));
        waypointCuracionAzul.Add(new Vector3(-21.8f, 0f, 102.4f));

        // Base Rojo y Azul
        waypointBaseRojo.Add(new Vector3(98.3f, 0f, -134.8f));
        waypointBaseAzul.Add(new Vector3(-114.6f, 0f, 105.4f));

        // Tactico Rojo
        waypointTacticoRojo.Add(new Vector3(112.1f, 0f, -7.3f));
        waypointTacticoRojo.Add(new Vector3(6.69f, 0f, -25.4f));
        waypointTacticoRojo.Add(new Vector3(-128.1f, 0f, -76.5f));

        // Tactico Azul
        waypointTacticoAzul.Add(new Vector3(-127.2f, 0f, -19.7f));
        waypointTacticoAzul.Add(new Vector3(112f, 0f, 46.9f));
        waypointTacticoAzul.Add(new Vector3(-22.8f, 0f, 2.13f));
    }

    public Vector3 GetWaypointReaparicion(Bando bando, int indice = 0)
    {
        if (bando == Bando.Rojo)
        {
            if (indice < waypointReaparicionRojo.Count)
                return waypointReaparicionRojo[indice];
            return waypointReaparicionRojo.Count > 0 ? waypointReaparicionRojo[0] : Vector3.zero;
        }
        
        if (indice < waypointReaparicionAzul.Count)
            return waypointReaparicionAzul[indice];
        return waypointReaparicionAzul.Count > 0 ? waypointReaparicionAzul[0] : Vector3.zero;
    }

    public Vector3 GetWaypointReaparicionMasCercano(Bando bando, Vector3 posicionReferencia)
    {
        List<Vector3> lista = (bando == Bando.Rojo) ? waypointReaparicionRojo : waypointReaparicionAzul;

        if (lista == null || lista.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 mejor = lista[0];
        float mejorDistancia = (mejor - posicionReferencia).sqrMagnitude;

        for (int i = 1; i < lista.Count; i++)
        {
            float distancia = (lista[i] - posicionReferencia).sqrMagnitude;
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = lista[i];
            }
        }

        return mejor;
    }

    public Vector3 GetWaypointCuracion(Bando bando, int indice = 0)
    {
        if (bando == Bando.Rojo)
        {
            if (indice < waypointCuracionRojo.Count)
                return waypointCuracionRojo[indice];
            return waypointCuracionRojo.Count > 0 ? waypointCuracionRojo[0] : Vector3.zero;
        }
        
        if (indice < waypointCuracionAzul.Count)
            return waypointCuracionAzul[indice];
        return waypointCuracionAzul.Count > 0 ? waypointCuracionAzul[0] : Vector3.zero;
    }

    public Vector3 GetWaypointBase(Bando bando, int indice = 0)
    {
        if (bando == Bando.Rojo)
        {
            if (indice < waypointBaseRojo.Count)
                return waypointBaseRojo[indice];
            return waypointBaseRojo.Count > 0 ? waypointBaseRojo[0] : Vector3.zero;
        }
        
        if (indice < waypointBaseAzul.Count)
            return waypointBaseAzul[indice];
        return waypointBaseAzul.Count > 0 ? waypointBaseAzul[0] : Vector3.zero;
    }

    public Vector3 GetWaypointTactico(Bando bando, int indice = 0)
    {
        if (bando == Bando.Rojo)
        {
            if (indice < waypointTacticoRojo.Count)
                return waypointTacticoRojo[indice];
            return waypointTacticoRojo.Count > 0 ? waypointTacticoRojo[0] : Vector3.zero;
        }

        if (indice < waypointTacticoAzul.Count)
            return waypointTacticoAzul[indice];
        return waypointTacticoAzul.Count > 0 ? waypointTacticoAzul[0] : Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (!debugWaypoints)
        {
            return;
        }

        Gizmos.color = new Color(0.85f, 0.2f, 0.2f);
        foreach (var wp in waypointBaseRojo)
            DrawWaypoint(wp, "R-Base");
        
        Gizmos.color = new Color(1f, 0.4f, 0.4f);
        foreach (var wp in waypointCuracionRojo)
            DrawWaypoint(wp, "R-Curacion");
        
        Gizmos.color = new Color(0.6f, 0.1f, 0.1f);
        foreach (var wp in waypointReaparicionRojo)
            DrawWaypoint(wp, "R-Reaparicion");

        Gizmos.color = new Color(1f, 0.8f, 0.1f);
        foreach (var wp in waypointTacticoRojo)
            DrawWaypoint(wp, "R-Tactico");

        Gizmos.color = new Color(0.2f, 0.5f, 0.95f);
        foreach (var wp in waypointBaseAzul)
            DrawWaypoint(wp, "A-Base");
        
        Gizmos.color = new Color(0.45f, 0.85f, 1f);
        foreach (var wp in waypointCuracionAzul)
            DrawWaypoint(wp, "A-Curacion");
        
        Gizmos.color = new Color(0.1f, 0.3f, 0.7f);
        foreach (var wp in waypointReaparicionAzul)
            DrawWaypoint(wp, "A-Reaparicion");

        Gizmos.color = new Color(0.2f, 0.95f, 0.5f);
        foreach (var wp in waypointTacticoAzul)
            DrawWaypoint(wp, "A-Tactico");
    }

    private void DrawWaypoint(Vector3 posicion, string etiqueta)
    {
        Gizmos.DrawSphere(posicion, radioGizmo);
        Gizmos.DrawWireSphere(posicion, radioGizmo * 1.4f);
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            posicion + Vector3.up * (radioGizmo + 0.4f),
            etiqueta
        );
#endif
    }
}
