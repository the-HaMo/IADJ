using UnityEngine;

// Pantalla de ayuda con todas las teclas del proyecto.
// Pulsa F1 para mostrar / ocultar.
//
// Sirve como referencia rapida en demos y para la documentacion (el enunciado
// pide "Mapa completo de las teclas + raton que usa vuestro programa").
public class HelpScreen : MonoBehaviour
{
    public KeyCode teclaToggle = KeyCode.F1;
    public bool visibleAlInicio = false;

    private bool visible;

    void Awake()
    {
        visible = visibleAlInicio;
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaToggle)) visible = !visible;
    }

    void OnGUI()
    {
        // Linea de ayuda permanente abajo a la izquierda
        GUIStyle hint = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        hint.normal.textColor = Color.white;
        GUI.Label(new Rect(10, Screen.height - 22, 200, 20), "F1: ayuda de teclas");

        if (!visible) return;

        float w = 420f;
        float h = 460f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h), "AYUDA — TECLAS Y RATON");

        GUIStyle titulo = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        titulo.normal.textColor = new Color(1f, 0.85f, 0.4f);

        GUIStyle item = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        item.normal.textColor = Color.white;

        float py = y + 30f;
        float px = x + 15f;
        float lineH = 17f;

        Linea(titulo, ref py, px, "Camara");
        Linea(item, ref py, px, "  W A S D       Mover camara");
        Linea(item, ref py, px, "  Shift         Movimiento rapido");
        Linea(item, ref py, px, "  Rueda raton   Zoom in/out");

        Linea(titulo, ref py, px, "Seleccion / control de unidad");
        Linea(item, ref py, px, "  Click izq.    Seleccionar unidad");
        Linea(item, ref py, px, "  Click izq.    (con unidad seleccionada) mover ahi");
        Linea(item, ref py, px, "  J / K         Debug danio / curacion a la unidad seleccionada");

        Linea(titulo, ref py, px, "Estrategia de bando");
        Linea(item, ref py, px, "  1 / 2         Bando ROJO Defensivo / Ofensivo");
        Linea(item, ref py, px, "  3 / 4         Bando AZUL Defensivo / Ofensivo");
        Linea(item, ref py, px, "  T             Toggle GUERRA TOTAL");

        Linea(titulo, ref py, px, "Mapa tactico y debug");
        Linea(item, ref py, px, "  M             Toggle minimapa de influencia");
        Linea(item, ref py, px, "  I             Toggle componente tactico del A*");
        Linea(item, ref py, px, "  G             Toggle visualizacion del grid");
        Linea(item, ref py, px, "  C             Toggle gizmos de percepcion / rutas");

        Linea(titulo, ref py, px, "Ventana de ayuda");
        Linea(item, ref py, px, "  F1            Mostrar / ocultar este panel");
    }

    private void Linea(GUIStyle st, ref float py, float px, string txt)
    {
        GUI.Label(new Rect(px, py, 600, 18), txt, st);
        py += st.fontSize + 4;
    }
}
