using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hospital : MonoBehaviour
{
    [Header("Ajustes de Curación")]
    public float curacionPorSegundo = 5f;
    public Bando bandoHospital = Bando.Rojo;

    private void OnTriggerStay(Collider npc)
    {
        NPCStats stats = npc.GetComponent<NPCStats>();

        if (stats != null && stats.miBando == bandoHospital)
        {
            if (stats.NecesitaCuracion())
            {
                Agent agent = npc.GetComponent<Agent>();
                
                if (agent != null && agent.Velocity.sqrMagnitude < 0.01f)
                {
                    stats.RecibirCuracion(curacionPorSegundo * Time.fixedDeltaTime);
                }
            }
        }
    }
}
