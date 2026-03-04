using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Patrón de formación defensiva.
/// Formación semicircular con el líder protegido en el centro.
/// </summary>
public class Defensa : FormationPattern
{
    public Defensa()
    {
        // Celda del líder (protegido en el centro-atrás)
        leaderSlot = (2, 3);
        
        // Celdas a usar por los NPCs (no se incluye la del líder)
        // Disposición semicircular defensiva
        validSlots = new[] { (1, 3), (3, 2), (0, 2), (3, 1), (0, 1), (2, 0), (1, 0) };
        
        // Orientación en cada celda (se incluye la del líder)
        // Diferentes orientaciones mirando hacia afuera para defender
        relativeAngles = new[] { 0f, 0f, 45f, -45f, 135f, -135f, 180f, 180f };
        
        this.numAgents = 8; // Líder + 7 defensores
    }
}
