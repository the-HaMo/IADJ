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
    public Transform baseRojo;
    public Transform baseAzul;

    // Getters basicos para el Spawner
    public Vector3 GetRandomReaparicion(Bando bando)
    {
        List<Transform> lista;
        if (bando == Bando.Rojo) { lista = spawnRojo; }
        else { lista = spawnAzul; }

        if (lista.Count > 0)
        {
            Transform t = lista[Random.Range(0, lista.Count)];
            return t.position;
        }
        return transform.position;
    }

    public Vector3 GetWaypointReaparicion(Bando bando, int index)
    {
        List<Transform> lista;
        if (bando == Bando.Rojo) { lista = spawnRojo; }
        else { lista = spawnAzul; }

        if (lista.Count > 0)
        {
            Transform t = lista[index % lista.Count];
            return t.position;
        }
        return transform.position;
    }

    public Vector3 GetWaypointReaparicionMasCercano(Bando bando, Vector3 posMuerte)
    {
        List<Transform> lista;
        if (bando == Bando.Rojo) { lista = spawnRojo; }
        else { lista = spawnAzul; }

        if (lista.Count == 0) return transform.position;

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
        if (bando == Bando.Rojo) { return hospitalRojo.position; }
        else { return hospitalAzul.position; }
    }

    public Vector3 GetBase(Bando bando)
    {
        if (bando == Bando.Rojo) { return baseRojo.position; }
        else { return baseAzul.position; }
    }

    public void DesactivarPuntoSpawn(Bando bando, GameObject puntoObj)
    {
        List<Transform> lista;
        if (bando == Bando.Rojo) { lista = spawnRojo; }
        else { lista = spawnAzul; }

        lista.RemoveAll(t => t == null || Vector3.Distance(t.position, puntoObj.transform.position) < 3f);
    }
}
