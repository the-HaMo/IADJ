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

        Gizmos.color = new Color(0.2f, 0.5f, 0.95f);
        foreach (var wp in waypointBaseAzul)
            DrawWaypoint(wp, "A-Base");
        
        Gizmos.color = new Color(0.45f, 0.85f, 1f);
        foreach (var wp in waypointCuracionAzul)
            DrawWaypoint(wp, "A-Curacion");
        
        Gizmos.color = new Color(0.1f, 0.3f, 0.7f);
        foreach (var wp in waypointReaparicionAzul)
            DrawWaypoint(wp, "A-Reaparicion");
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
