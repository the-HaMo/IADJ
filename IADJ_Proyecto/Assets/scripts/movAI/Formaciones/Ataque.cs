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
        // Ataque de 6 NPCs:
        // - 4 delante
        // - 2 detrás
        // - el lider es el de atras a la izquierda.
        this.leaderSlot = (1, 2);

        // Orden de asignacion para los 5 seguidores:
        // 4 del frente + 1 atras derecha.
        validSlots = new[]
        {
            (0, 1), // frente izquierda
            (1, 3), // frente centro-izquierda
            (2, 3), // frente centro-derecha
            (3, 1), // frente derecha
            (2, 2)  // atras derecha
        };

        // [0] lider, [1..5] seguidores.
        relativeAngles = new[]
        {
            180f,  // lider (atras izquierda) mirando hacia atras
            -45f,   // frente izquierda mirando hacia su lado
            0f,    // frente centro-izquierda
            0f,    // frente centro-derecha
            45f,  // frente derecha mirando hacia su lado
            180f   // atras derecha mirando hacia atras
        };

        this.numAgents = 6; // Lider + 5 seguidores
    }
}
