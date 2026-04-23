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
        // Reaparicion Rojo
        if (waypointReaparicionRojo.Count == 0)
        {
            waypointReaparicionRojo.Add(new Vector3(70f, 0f, 20f));
            waypointReaparicionRojo.Add(new Vector3(50f, 0f, 10f));
            waypointReaparicionRojo.Add(new Vector3(80f, 0f, 35f));
        }

        // Reaparicion Azul
        if (waypointReaparicionAzul.Count == 0)
        {
            waypointReaparicionAzul.Add(new Vector3(19f, 0f, 70f));
            waypointReaparicionAzul.Add(new Vector3(43f, 0f, 74f));
            waypointReaparicionAzul.Add(new Vector3(10f, 0f, 50f));
        }

        // Curacion Rojo
        if (waypointCuracionRojo.Count == 0)
        {
            waypointCuracionRojo.Add(new Vector3(19f, 0f, 13.5f));
        }

        // Curacion Azul
        if (waypointCuracionAzul.Count == 0)
        {
            waypointCuracionAzul.Add(new Vector3(73f, 0f, 77f));
        }

        // Base Rojo
        if (waypointBaseRojo.Count == 0)
        {
            waypointBaseRojo.Add(new Vector3(70f, 0f, 20f));
        }

        // Base Azul
        if (waypointBaseAzul.Count == 0)
        {
            waypointBaseAzul.Add(new Vector3(19f, 0f, 70f));
        }
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
