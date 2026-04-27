using UnityEngine;

public class Torre : MonoBehaviour
{
    [Header("Configuracion")]
    public Bando bandoPropietario;
    public float tiempoParaConquistar = 20f;

    private bool conquistada = false;
    private float progresoCaptura = 0f;
    private bool recibioDanioAlguien = false;
    private float tiempoUltimaVezVisto = 0f;
    private NPCStats atacanteActual = null;
    private float tiempoSiguienteLog = 0f;

    private void OnTriggerStay(Collider other)
    {
        if (conquistada)
        {
            return;
        }

        NPCStats stats = other.GetComponentInParent<NPCStats>();
        if (stats != null)
        {
            if (stats.miBando != bandoPropietario)
            {
                tiempoUltimaVezVisto = Time.time;
                
                if (atacanteActual != stats)
                {
                    if (atacanteActual != null)
                    {
                        atacanteActual.OnDanioRecibido -= MarcarDanio;
                    }
                    atacanteActual = stats;
                    atacanteActual.OnDanioRecibido += MarcarDanio;
                    Debug.Log("Captura iniciada por: " + stats.name);
                }
            }
        }
    }

    void Update()
    {
        if (conquistada)
        {
            return;
        }

        // Si en 0.2s no vemos a nadie, reseteamos progreso
        if (Time.time - tiempoUltimaVezVisto > 0.2f)
        {
            if (progresoCaptura > 0)
            {
                progresoCaptura = 0f;
                if (atacanteActual != null)
                {
                    atacanteActual.OnDanioRecibido -= MarcarDanio;
                }
                atacanteActual = null;
                Debug.Log("Zona vacia. Progreso reseteado.");
            }
            return;
        }

        if (recibioDanioAlguien)
        {
            progresoCaptura = 0f;
            recibioDanioAlguien = false;
            Debug.Log("¡Reset por daño!");
            return;
        }

        progresoCaptura += Time.deltaTime;

        if (Time.time >= tiempoSiguienteLog)
        {
            int pct = Mathf.RoundToInt((progresoCaptura / tiempoParaConquistar) * 100);
            Debug.Log("Capturando Torre... " + pct + "%");
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
        
        if (atacanteActual != null)
        {
            atacanteActual.OnDanioRecibido -= MarcarDanio;
        }
        
        Debug.Log("<color=green>¡TORRE CONQUISTADA!</color>");

        WayPoints wp = FindFirstObjectByType<WayPoints>();
        if (wp != null)
        {
            wp.DesactivarPuntoSpawn(bandoPropietario, gameObject);
        }

        Destroy(gameObject);
    }
}
