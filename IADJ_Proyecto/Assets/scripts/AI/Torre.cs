using UnityEngine;

public class Torre : MonoBehaviour
{
    [Header("Configuracion")]
    public Bando bandoPropietario;
    public float tiempoParaConquistar = 20f;

    private bool conquistada = false;
    private float progresoCaptura = 0f;
    private bool recibioDanioAlguien = false;
    private float tiempoSiguienteLog = 0f;

    private System.Collections.Generic.HashSet<NPCStats> atacantesEnZona = new System.Collections.Generic.HashSet<NPCStats>();

    private void OnTriggerEnter(Collider other)
    {
        if (conquistada) return;

        NPCStats stats = other.GetComponentInParent<NPCStats>();
        if (stats != null && stats.miBando != bandoPropietario)
        {
            if (atacantesEnZona.Add(stats))
            {
                stats.OnDanioRecibido += MarcarDanio;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (conquistada) return;

        NPCStats stats = other.GetComponentInParent<NPCStats>();
        if (stats != null && atacantesEnZona.Contains(stats))
        {
            atacantesEnZona.Remove(stats);
            stats.OnDanioRecibido -= MarcarDanio;
        }
    }

    void Update()
    {
        if (conquistada) return;

        // Limpiar atacantes que hayan muerto (destruidos)
        atacantesEnZona.RemoveWhere(s => s == null);

        // Si no hay nadie capturando, resetear
        if (atacantesEnZona.Count == 0)
        {
            if (progresoCaptura > 0)
            {
                progresoCaptura = 0f;
                Debug.Log($"Zona {name} vacia. Progreso reseteado.");
            }
            return;
        }

        // Si alguien recibio daño, se interrumpe la captura
        if (recibioDanioAlguien)
        {
            progresoCaptura = 0f;
            recibioDanioAlguien = false;
            Debug.Log($"¡Captura de {name} interrumpida por daño!");
            return;
        }

        // Progresar captura
        progresoCaptura += Time.deltaTime;

        if (Time.time >= tiempoSiguienteLog)
        {
            int pct = Mathf.RoundToInt((progresoCaptura / tiempoParaConquistar) * 100);
            Debug.Log($"Capturando {name}... {pct}%");
            tiempoSiguienteLog = Time.time + 1f;
        }

        if (progresoCaptura >= tiempoParaConquistar)
        {
            Conquistar();
        }
    }

    private void MarcarDanio()
    {
        recibioDanioAlguien = true;
    }

    private void Conquistar()
    {
        conquistada = true;
        
        foreach (var atacante in atacantesEnZona)
        {
            if (atacante != null) atacante.OnDanioRecibido -= MarcarDanio;
        }
        atacantesEnZona.Clear();
        
        Bando bandoConquistador = (bandoPropietario == Bando.Rojo) ? Bando.Azul : Bando.Rojo;
        Debug.Log($"<color=green>¡TORRE CONQUISTADA! El bando {bandoConquistador} ha tomado el control de {gameObject.name}.</color>");

        WayPoints wp = FindFirstObjectByType<WayPoints>();
        if (wp != null)
        {
            wp.DesactivarPuntoSpawn(bandoPropietario, gameObject);
        }

        Destroy(gameObject);
    }
}
