using UnityEngine;

// Pinta el mapa tactico activo en una esquina de la pantalla.
// Cumple los requisitos (e) del Bloque 2 ampliado a tres mapas:
//   - Influencia: heatmap rojo / azul (cual bando domina la zona)
//   - Tension:    cuanto "ruido" total hay (suma de ambas influencias)
//   - Vulnerabilidad: zonas DISPUTADAS (ambos bandos balanceados)
//
// La M cicla entre los tres mapas via EstadoTacticoGlobal.CiclarMapa().
public class MinimapaInfluencia : MonoBehaviour
{
    [Header("Posicion y tamano")]
    public Vector2 posicion = new Vector2(20, 20);
    public Vector2 tamano = new Vector2(200, 200);

    [Header("Refresco")]
    public float intervaloRefresco = 0.5f;

    [Header("Visualizacion")]
    [Tooltip("Intensidad maxima esperada para influencia neta.")]
    public float intensidadMaxInfluencia = 8f;
    [Tooltip("Intensidad maxima esperada para tension (suele ser mayor que influencia neta).")]
    public float intensidadMaxTension = 16f;
    [Tooltip("Intensidad maxima esperada para vulnerabilidad. Como mucho == 2*min, suele ser baja.")]
    public float intensidadMaxVulnerabilidad = 6f;

    [Tooltip("Mostrar etiqueta con el nombre del mapa actual.")]
    public bool mostrarEtiqueta = true;

    public bool visible = true;

    private MapaInfluencia mapa;
    private GridManager grid;
    private Texture2D textura;
    private float nextUpdate;
    private EstadoTacticoGlobal.ModoMapaTactico ultimoModoPintado = (EstadoTacticoGlobal.ModoMapaTactico)(-1);

    void Awake()
    {
        mapa = FindFirstObjectByType<MapaInfluencia>();
        grid = FindFirstObjectByType<GridManager>();
    }

    void OnEnable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado += AlCambiarEstadoGlobal;
    }

    void OnDisable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado -= AlCambiarEstadoGlobal;
    }

    private void AlCambiarEstadoGlobal()
    {
        // Forzar repintado inmediato cuando cambia el modo de mapa
        if (EstadoTacticoGlobal.MapaActual != ultimoModoPintado)
        {
            nextUpdate = 0f;
        }
    }

    void Update()
    {
        if (!visible) return;

        if (Time.time >= nextUpdate)
        {
            ActualizarTextura();
            nextUpdate = Time.time + intervaloRefresco;
        }
    }

    private void ActualizarTextura()
    {
        if (mapa == null || grid == null) return;

        int sx = grid.GridSizeX;
        int sy = grid.GridSizeY;

        if (textura == null || textura.width != sx || textura.height != sy)
        {
            textura = new Texture2D(sx, sy, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
        }

        var modo = EstadoTacticoGlobal.MapaActual;
        ultimoModoPintado = modo;

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                Node n = grid.GetNode(x, y);
                Color c;

                if (n != null && !n.isWalkable)
                {
                    c = new Color(0.1f, 0.1f, 0.1f, 1f); // obstaculo
                }
                else
                {
                    switch (modo)
                    {
                        case EstadoTacticoGlobal.ModoMapaTactico.Influencia:
                            c = ColorInfluencia(x, y, n);
                            break;
                        case EstadoTacticoGlobal.ModoMapaTactico.Tension:
                            c = ColorTension(x, y, n);
                            break;
                        case EstadoTacticoGlobal.ModoMapaTactico.Vulnerabilidad:
                            c = ColorVulnerabilidad(x, y, n);
                            break;
                        default:
                            c = ColorPorBioma(n != null ? n.bioma : Bioma.Pradera);
                            break;
                    }
                }

                textura.SetPixel(x, y, c);
            }
        }

        textura.Apply();
    }

    // ------- INFLUENCIA: rojo si gana Rojo, azul si gana Azul ----------
    private Color ColorInfluencia(int x, int y, Node n)
    {
        float neto = mapa.GetInfluenciaNeta(x, y);
        float norm = Mathf.Clamp(Mathf.Abs(neto) / Mathf.Max(0.01f, intensidadMaxInfluencia), 0f, 1f);

        if (Mathf.Abs(neto) < 0.05f)
        {
            return ColorPorBioma(n != null ? n.bioma : Bioma.Pradera);
        }
        if (neto > 0)
        {
            return Color.Lerp(new Color(1f, 0.6f, 0.6f, 1f), Color.red, norm);
        }
        return Color.Lerp(new Color(0.6f, 0.6f, 1f, 1f), Color.blue, norm);
    }

    // ------- TENSION: gradiente de gris a amarillo / naranja ------------
    // Mide la suma de ambas influencias: zonas con mucha actividad (de uno o ambos).
    private Color ColorTension(int x, int y, Node n)
    {
        float t = mapa.GetTension(x, y);
        float norm = Mathf.Clamp(t / Mathf.Max(0.01f, intensidadMaxTension), 0f, 1f);

        if (norm < 0.02f)
        {
            return ColorPorBioma(n != null ? n.bioma : Bioma.Pradera);
        }
        // 0 -> amarillo claro, 1 -> rojo intenso (zona muy "ruidosa")
        Color baja = new Color(1f, 0.95f, 0.5f, 1f);  // amarillo
        Color alta = new Color(0.9f, 0.25f, 0f, 1f);  // naranja-rojo
        return Color.Lerp(baja, alta, norm);
    }

    // ------- VULNERABILIDAD: gradiente verde -> magenta -----------------
    // Picos: zonas donde ambos bandos estan balanceados (frente de batalla).
    private Color ColorVulnerabilidad(int x, int y, Node n)
    {
        float v = mapa.GetVulnerabilidad(x, y);
        float norm = Mathf.Clamp(v / Mathf.Max(0.01f, intensidadMaxVulnerabilidad), 0f, 1f);

        if (norm < 0.02f)
        {
            return ColorPorBioma(n != null ? n.bioma : Bioma.Pradera);
        }
        // 0 -> blanco, 1 -> magenta saturado (zona muy disputada)
        Color baja = new Color(0.95f, 0.95f, 0.95f, 1f);
        Color alta = new Color(0.9f, 0f, 0.9f, 1f);
        return Color.Lerp(baja, alta, norm);
    }

    private Color ColorPorBioma(Bioma b)
    {
        switch (b)
        {
            case Bioma.Bosque:  return new Color(0.2f, 0.45f, 0.2f, 1f);
            case Bioma.Camino:  return new Color(0.6f, 0.5f, 0.3f, 1f);
            case Bioma.Urbano:  return new Color(0.5f, 0.5f, 0.55f, 1f);
            default:            return new Color(0.45f, 0.7f, 0.45f, 1f);
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        // Marco
        GUI.Box(new Rect(posicion.x - 4, posicion.y - 4, tamano.x + 8, tamano.y + 8), "");

        if (textura != null)
        {
            Rect r = new Rect(posicion.x, posicion.y, tamano.x, tamano.y);
            // Rotar 180 grados: invertimos tanto X (U) como Y (V)
            GUI.DrawTextureWithTexCoords(r, textura, new Rect(1, 1, -1, -1));
        }

        if (mostrarEtiqueta)
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            estilo.normal.textColor = Color.white;
            string txt = "";
            GUI.Label(new Rect(posicion.x + 6, posicion.y + 2, tamano.x, 22), txt, estilo);
        }
    }
}
