using UnityEngine;

public enum ModoEstrategico
{
    Defensivo,   // Las unidades no avanzan, defienden zonas propias
    Ofensivo,    // Las unidades buscan activamente al enemigo / la base enemiga
    GuerraTotal  // Modo "guerra total": todos a por la victoria sin mirar atras
}

// Controla el modo estrategico de cada bando a nivel global.
// PercepcionNPC consulta esto para sesgar el comportamiento individual.
//
// Teclas (configurables):
//  1 -> Rojo Defensivo      2 -> Rojo Ofensivo
//  3 -> Azul Defensivo      4 -> Azul Ofensivo
//  T -> Toggle GUERRA TOTAL (aplica a ambos bandos)
public class EstrategiaBando : MonoBehaviour
{
    public static EstrategiaBando Instance { get; private set; }

    [Header("Modos por bando")]
    public ModoEstrategico modoRojo = ModoEstrategico.Defensivo;
    public ModoEstrategico modoAzul = ModoEstrategico.Defensivo;

    [Header("Guerra Total")]
    public bool guerraTotal = false;

    [Header("Teclas")]
    public KeyCode teclaRojoDef = KeyCode.Alpha1;
    public KeyCode teclaRojoOfe = KeyCode.Alpha2;
    public KeyCode teclaAzulDef = KeyCode.Alpha3;
    public KeyCode teclaAzulOfe = KeyCode.Alpha4;
    public KeyCode teclaGuerraTotal = KeyCode.T;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaRojoDef)) { modoRojo = ModoEstrategico.Defensivo; LogCambio(Bando.Rojo); }
        if (Input.GetKeyDown(teclaRojoOfe)) { modoRojo = ModoEstrategico.Ofensivo;  LogCambio(Bando.Rojo); }
        if (Input.GetKeyDown(teclaAzulDef)) { modoAzul = ModoEstrategico.Defensivo; LogCambio(Bando.Azul); }
        if (Input.GetKeyDown(teclaAzulOfe)) { modoAzul = ModoEstrategico.Ofensivo;  LogCambio(Bando.Azul); }

        if (Input.GetKeyDown(teclaGuerraTotal))
        {
            guerraTotal = !guerraTotal;
            Debug.Log($"<color=red>=== GUERRA TOTAL: {(guerraTotal ? "ACTIVADA" : "DESACTIVADA")} ===</color>");
        }
    }

    public ModoEstrategico GetModo(Bando bando)
    {
        if (guerraTotal) return ModoEstrategico.GuerraTotal;
        return bando == Bando.Rojo ? modoRojo : modoAzul;
    }

    private void LogCambio(Bando bando)
    {
        ModoEstrategico m = bando == Bando.Rojo ? modoRojo : modoAzul;
        Debug.Log($"[Estrategia] {bando} -> {m}");
    }

    void OnGUI()
    {
        // Indicador en pantalla del modo actual
        GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };

        if (guerraTotal)
        {
            estilo.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width - 240, 10, 230, 20), "*** GUERRA TOTAL ***", estilo);
        }
        else
        {
            estilo.normal.textColor = new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(Screen.width - 240, 10, 230, 20), $"Rojo: {modoRojo}", estilo);
            estilo.normal.textColor = new Color(0.4f, 0.6f, 1f);
            GUI.Label(new Rect(Screen.width - 240, 30, 230, 20), $"Azul: {modoAzul}", estilo);
        }

        estilo.normal.textColor = Color.white;
        estilo.fontSize = 10;
        estilo.fontStyle = FontStyle.Normal;
        GUI.Label(new Rect(Screen.width - 240, 55, 230, 60), "1/2: Rojo Def/Ofe\n3/4: Azul Def/Ofe\nT: Guerra Total", estilo);
    }
}
