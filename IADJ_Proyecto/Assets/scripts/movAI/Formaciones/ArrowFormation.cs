using UnityEngine;

public class ArrowFormation : FormationPattern
{
    [Header("Configuración de la Flecha")]
    [Tooltip("Separación lateral entre personajes del mismo nivel")]
    public float lateralSpacing = 3.0f;
    
    [Tooltip("Separación hacia atrás entre niveles")]
    public float depthSpacing = 2.5f;

    private void Awake()
    {
        numberOfSlots = 7; // Exactamente 7 slots
    }

    public override Pose GetSlotLocation(int slotNumber)
    {
        Vector3 position = Vector3.zero;
        float orientationDegrees = 0f;

        switch (slotNumber)
        {
            case 0: // Líder - punta de la flecha
                position = new Vector3(0, 0, 0);
                orientationDegrees = 0f;
                break;

            case 1: // Nivel 1 - izquierda
                position = new Vector3(-lateralSpacing, 0, -depthSpacing);
                orientationDegrees = 15f; // Ligeramente hacia adentro
                break;

            case 2: // Nivel 1 - derecha
                position = new Vector3(lateralSpacing, 0, -depthSpacing);
                orientationDegrees = -15f; // Ligeramente hacia adentro
                break;

            case 3: // Nivel 2 - izquierda
                position = new Vector3(-lateralSpacing * 1.8f, 0, -depthSpacing * 2.2f);
                orientationDegrees = 30f; // Más ángulo hacia adentro
                break;

            case 4: // Nivel 2 - derecha
                position = new Vector3(lateralSpacing * 1.8f, 0, -depthSpacing * 2.2f);
                orientationDegrees = -30f; // Más ángulo hacia adentro
                break;

            case 5: // Nivel 3 - izquierda (alas externas)
                position = new Vector3(-lateralSpacing * 2.5f, 0, -depthSpacing * 3.5f);
                orientationDegrees = 45f; // Máximo ángulo hacia afuera
                break;

            case 6: // Nivel 3 - derecha (alas externas)
                position = new Vector3(lateralSpacing * 2.5f, 0, -depthSpacing * 3.5f);
                orientationDegrees = -45f; // Máximo ángulo hacia afuera
                break;

            default:
                Debug.LogWarning($"ArrowFormation: Slot {slotNumber} fuera de rango");
                break;
        }

        Quaternion rotation = Quaternion.Euler(0, orientationDegrees, 0);
        return new Pose(position, rotation);
    }

    public override bool SupportsSlots(int slotCount)
    {
        return slotCount <= numberOfSlots;
    }
}
