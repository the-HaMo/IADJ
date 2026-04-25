using UnityEngine;

public class Hospital : MonoBehaviour
{
    [Header("Ajustes de Curación")]
    public float curacionPorSegundo = 5f;
    public Bando bandoHospital = Bando.Rojo;
    public float radioDeteccion = 5f; // Radio dentro del cual buscar NPCs
    
    private Collider hospitalCollider;

    private void Start()
    {
        hospitalCollider = GetComponent<Collider>();
        if (hospitalCollider == null)
        {
            // Debug.LogError($"[HOSPITAL] ¡ERROR! Hospital {name} NO tiene Collider", gameObject);
        }
    }

    private void Update()
    {
        if (hospitalCollider == null) return;
        
        // Buscar todos los NPCs dentro del radio de detección
        Collider[] collidersEnRango = Physics.OverlapSphere(transform.position, radioDeteccion);
        
        foreach (Collider collider in collidersEnRango)
        {
            NPCStats stats = collider.GetComponent<NPCStats>();
            if (stats == null)
            {
                continue;
            }

            // Verificar si el NPC es del mismo bando
            if (stats.miBando != bandoHospital)
            {
                continue;
            }

            // Verificar si necesita curación
            if (!stats.NecesitaCuracion())
            {
                continue;
            }

            // CURAR AL NPC
            stats.RecibirCuracion(curacionPorSegundo * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar el radio de detección en el editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}
