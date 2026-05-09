using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TacticalCanvasController : MonoBehaviour
{
    [Header("Texto del panel")]
    [SerializeField] private Text textoEstado;

    [Header("Atajos")]
    [SerializeField] private KeyCode teclaDefensa = KeyCode.Q;
    [SerializeField] private KeyCode teclaAtaque = KeyCode.E;
    [SerializeField] private KeyCode teclaMapa = KeyCode.M;
    [SerializeField] private KeyCode teclaGuerraTotal = KeyCode.T;
    [SerializeField] private KeyCode teclaDebug = KeyCode.B;
    [SerializeField] private KeyCode teclaPathfinding = KeyCode.P;

    private void Awake()
    {
        if (textoEstado == null)
        {
            textoEstado = GetComponentInChildren<Text>(true);
        }
    }

    private void OnEnable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado += RefrescarTexto;
    }

    private void OnDisable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado -= RefrescarTexto;
    }

    private void Start()
    {
        AplicarModoCombateAUnidades();
        RefrescarTexto();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaDefensa))
        {
            EstadoTacticoGlobal.FijarModoCombate(EstadoNPC.Defensa);
            AplicarModoCombateAUnidades();
        }
        else if (Input.GetKeyDown(teclaAtaque))
        {
            EstadoTacticoGlobal.FijarModoCombate(EstadoNPC.Ataque);
            AplicarModoCombateAUnidades();
        }

        if (Input.GetKeyDown(teclaMapa))
        {
            EstadoTacticoGlobal.CiclarMapa();
        }

        if (Input.GetKeyDown(teclaGuerraTotal))
        {
            EstadoTacticoGlobal.AlternarGuerraTotal();
        }

        if (Input.GetKeyDown(teclaDebug))
        {
            EstadoTacticoGlobal.AlternarDebug();
        }

        if (Input.GetKeyDown(teclaPathfinding))
        {
            EstadoTacticoGlobal.AlternarPathfindingTactico();
        }
    }

    private void AplicarModoCombateAUnidades()
    {
        estadoNPC[] unidades = FindObjectsByType<estadoNPC>(FindObjectsSortMode.None);

        List<estadoNPC> rojos = new List<estadoNPC>();
        List<estadoNPC> azules = new List<estadoNPC>();

        foreach (estadoNPC unidad in unidades)
        {
            if (unidad == null)
            {
                continue;
            }

            NPCStats stats = unidad.GetComponent<NPCStats>();
            if (stats == null)
            {
                continue;
            }

            if (stats.miBando == Bando.Rojo) rojos.Add(unidad);
            else if (stats.miBando == Bando.Azul) azules.Add(unidad);
        }

        if (EstadoTacticoGlobal.ModoCombateActual == EstadoNPC.Ataque)
        {
            // Modo ataque: 8 atacan, 4 vigilan, 3 defienden.
            AplicarDistribucion(rojos, 8, 4, 3, EstadoNPC.Ataque);
            AplicarDistribucion(azules, 8, 4, 3, EstadoNPC.Ataque);
        }
        else
        {
            // Modo defensa: 4 atacan, 6 vigilan, 5 defienden.
            AplicarDistribucion(rojos, 3, 6, 6, EstadoNPC.Defensa);
            AplicarDistribucion(azules, 3, 6, 6, EstadoNPC.Defensa);
        }
    }

    private void AplicarDistribucion(List<estadoNPC> unidades, int cupoAtaque, int cupoVigilancia, int cupoDefensa, EstadoNPC modoPrincipal)
    {
        if (unidades == null || unidades.Count == 0)
        {
            return;
        }

        int totalDeseado = cupoAtaque + cupoVigilancia + cupoDefensa;
        int vivos = unidades.Count;

        if (vivos < totalDeseado)
        {
            float ratio = vivos / (float)totalDeseado;
            cupoAtaque = Mathf.FloorToInt(cupoAtaque * ratio);
            cupoVigilancia = Mathf.FloorToInt(cupoVigilancia * ratio);
            cupoDefensa = Mathf.FloorToInt(cupoDefensa * ratio);

            int asignados = cupoAtaque + cupoVigilancia + cupoDefensa;
            int restantes = vivos - asignados;

            if (restantes > 0)
            {
                if (modoPrincipal == EstadoNPC.Ataque) cupoAtaque += restantes;
                else if (modoPrincipal == EstadoNPC.Defensa) cupoDefensa += restantes;
                else cupoVigilancia += restantes;
            }
        }

        int idx = 0;

        for (int i = 0; i < cupoAtaque && idx < vivos; i++, idx++)
        {
            unidades[idx].SetEstado(EstadoNPC.Ataque);
        }

        for (int i = 0; i < cupoVigilancia && idx < vivos; i++, idx++)
        {
            unidades[idx].SetEstado(EstadoNPC.Vigilancia);
        }

        for (int i = 0; i < cupoDefensa && idx < vivos; i++, idx++)
        {
            unidades[idx].SetEstado(EstadoNPC.Defensa);
        }

        while (idx < vivos)
        {
            unidades[idx].SetEstado(modoPrincipal);
            idx++;
        }
    }

    private void RefrescarTexto()
    {
        if (textoEstado == null)
        {
            return;
        }

        string modoCombate = EstadoTacticoGlobal.ModoCombateActual == EstadoNPC.Defensa ? "Defensa" : "Ataque";
        string mapa = EstadoTacticoGlobal.ObtenerTextoMapaActual();
        string guerraTotal = FormatearEstado(EstadoTacticoGlobal.GuerraTotalActiva);
        string debug = FormatearEstado(EstadoTacticoGlobal.DebugActivo);
        string pathfinding = FormatearEstado(EstadoTacticoGlobal.PathfindingTacticoActivo);

        textoEstado.text =
            $"Modo (Q/E): {modoCombate}\n" +
            $"Mapa (M): {mapa}\n" +
            $"Guerra total (T): {guerraTotal}\n" +
            $"Debugging (B): {debug}\n" +
            $"PathFinding Tactico (P): {pathfinding}\n";
    }

    private static string FormatearEstado(bool activo)
    {
        return activo ? "<color=#3DEB6B>\u2714</color>" : "<color=#E05A5A>\u2716</color>";
    }
}
