using System;
using UnityEngine;

public static class EstadoTacticoGlobal
{
    public enum ModoMapaTactico
    {
        Influencia,
        Vulnerabilidad,
        Tension,
    
    }

    public static event Action OnEstadoCambiado;

    public static EstadoNPC ModoCombateActual { get; private set; } = EstadoNPC.Defensa;
    public static ModoMapaTactico MapaActual { get; private set; } = ModoMapaTactico.Influencia;
    public static bool GuerraTotalActiva { get; private set; }
    public static bool DebugActivo { get; private set; }
    public static bool PathfindingTacticoActivo { get; private set; } = true;

    public static void AlternarModoCombate()
    {
        ModoCombateActual = ModoCombateActual == EstadoNPC.Defensa ? EstadoNPC.Ataque : EstadoNPC.Defensa;
        NotificarCambio();
    }

    public static void FijarModoCombate(EstadoNPC nuevoModo)
    {
        if (ModoCombateActual == nuevoModo)
        {
            return;
        }

        ModoCombateActual = nuevoModo;
        NotificarCambio();
    }

    public static void CiclarMapa()
    {
        int totalMapas = Enum.GetValues(typeof(ModoMapaTactico)).Length;
        int siguiente = ((int)MapaActual + 1) % totalMapas;
        MapaActual = (ModoMapaTactico)siguiente;
        NotificarCambio();
    }

    public static void AlternarGuerraTotal()
    {
        GuerraTotalActiva = !GuerraTotalActiva;
        NotificarCambio();
    }

    public static void AlternarDebug()
    {
        DebugActivo = !DebugActivo;
        NotificarCambio();
    }

    public static void AlternarPathfindingTactico()
    {
        PathfindingTacticoActivo = !PathfindingTacticoActivo;
        NotificarCambio();
    }

    public static string ObtenerTextoMapaActual()
    {
        return MapaActual switch
        {
            ModoMapaTactico.Influencia => "Influencia",
            ModoMapaTactico.Vulnerabilidad => "Vulnerabilidad",
            ModoMapaTactico.Tension => "Tension",
            _ => "Desconocido"
        };
    }

    public static bool EsMapaInfluenciaActivo()
    {
        return MapaActual == ModoMapaTactico.Influencia;
    }

    private static void NotificarCambio()
    {
        OnEstadoCambiado?.Invoke();
    }
}
