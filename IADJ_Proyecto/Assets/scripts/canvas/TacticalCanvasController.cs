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
        foreach (estadoNPC unidad in unidades)
        {
            if (unidad == null)
            {
                continue;
            }

            unidad.SetEstado(EstadoTacticoGlobal.ModoCombateActual);
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