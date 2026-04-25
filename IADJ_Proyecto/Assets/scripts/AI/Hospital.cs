using UnityEngine;

public class Hospital : MonoBehaviour
{
    public float curacionPorSegundo = 50f;
    public Bando bandoHospital;

    private int capaUnidad;

    void Awake()
    {
        capaUnidad = LayerMask.NameToLayer("Unidad");
    }

    private void OnTriggerStay(Collider other)
    {
        // Verificamos si es la capa correcta
        if (other.gameObject.layer != capaUnidad) return;

        NPCStats stats = other.GetComponentInParent<NPCStats>();
        if (stats != null && stats.miBando == bandoHospital && stats.NecesitaCuracion())
        {
            stats.RecibirCuracion(curacionPorSegundo * Time.deltaTime);
            Debug.Log($"Curando a {stats.name}: {stats.VidaActual:F0}/{stats.VidaMax}");
        }
    }
}
