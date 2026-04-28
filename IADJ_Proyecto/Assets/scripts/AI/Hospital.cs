using UnityEngine;

public class Hospital : MonoBehaviour
{
    public float curacionPorSegundo = 50f;
    public Bando bandoHospital;

    private int capaUnidad;

    void Awake()
    {
        capaUnidad = LayerMask.NameToLayer("Unidad");
        if (capaUnidad < 0)
        {
            Debug.LogWarning($"[Hospital] La layer 'Unidad' no existe en este proyecto. " +
                             $"El hospital '{name}' no curara a nadie. Crea la layer en Edit > Project Settings > Tags and Layers.", this);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Si la layer no existe, capaUnidad == -1 y siempre fallaria. Mejor usar GetComponentInParent.
        if (capaUnidad >= 0 && other.gameObject.layer != capaUnidad) return;

        NPCStats stats = other.GetComponentInParent<NPCStats>();
        if (stats != null && stats.miBando == bandoHospital && stats.NecesitaCuracion())
        {
            stats.RecibirCuracion(curacionPorSegundo * Time.deltaTime);
            Debug.Log($"Curando a {stats.name}: {stats.VidaActual:F0}/{stats.VidaMax}");
        }
    }
}
