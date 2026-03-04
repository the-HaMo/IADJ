using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Patrón de formación ofensiva.
/// Formación en cuña para ataque frontal.
/// </summary>
public class Ataque : FormationPattern
{
    public Ataque()
    {
        // Celda del líder (adelante en el centro)
        this.leaderSlot = (2, 1);
        
        // Celdas a usar por los NPCs (no se incluye la del líder)
        // Disposición en cuña hacia adelante
        validSlots = new[] { (1, 3), (2, 3), (3, 2), (2, 2), (1, 2), (0, 2), (1, 1), (2, 0), (1, 0) };
        
        // Orientación en cada celda (se incluye la del líder)
        // Diferentes orientaciones para cumplir requisito de al menos 3 orientaciones distintas
        relativeAngles = new[] { 0f, 30f, -30f, -30f, 0f, 0f, 30f, 0f, 60f, -60f };
        
        this.numAgents = 10; // Líder + 9 seguidores
    }
}
