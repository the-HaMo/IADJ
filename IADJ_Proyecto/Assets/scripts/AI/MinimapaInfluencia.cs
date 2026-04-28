using UnityEngine;

// Pinta el mapa de influencia como un heatmap rojo/azul en una esquina de la
// pantalla. Cumple el requisito (e) del Bloque 2.
//
// Tecla M: toggle visibilidad
public class MinimapaInfluencia : MonoBehaviour
{
    [Header("Posicion y tamano")]
    public Vector2 posicion = new Vector2(20, 20);
    public Vector2 tamano = new Vector2(220, 220);

    [Header("Refresco")]
    public float intervaloRefresco = 0.5f;

    [Header("Visualizacion")]
    [Tooltip("Intensidad maxima esperada (para normalizar el color).")]
    public float intensidadMax = 8f;
    public bool dibujarUnidades = true;

    [Header("Tecla de toggle")]
    public KeyCode teclaToggle = KeyCode.M;
    public bool visible = true;

    private MapaInfluencia mapa;
    private GridManager grid;
    private Texture2D textura;
    private float nextUpdate;

    void Awake()
    {
        mapa = FindFirstObjectByType<MapaInfluencia>();
        grid = FindFirstObjectByType<GridManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaToggle)) visible = !visible;
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
                    float r = mapa.GetInfluencia(Bando.Rojo, x, y);
                    float a = mapa.GetInfluencia(Bando.Azul, x, y);
                    float neto = r - a;
                    float norm = Mathf.Clamp(Mathf.Abs(neto) / Mathf.Max(0.01f, intensidadMax), 0f, 1f);

                    if (Mathf.Abs(neto) < 0.05f)
                    {
                        // Sin influencia: gris segun bioma
                        c = ColorPorBioma(n != null ? n.bioma : Bioma.Pradera);
                    }
                    else if (neto > 0)
                    {
                        c = Color.Lerp(new Color(1f, 0.6f, 0.6f, 1f), Color.red, norm);
                    }
                    else
                    {
                        c = Color.Lerp(new Color(0.6f, 0.6f, 1f, 1f), Color.blue, norm);
                    }
                }

                textura.SetPixel(x, y, c);
            }
        }

        textura.Apply();
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
        if (!visible || textura == null) return;

        // Marco
        GUI.Box(new Rect(posicion.x - 4, posicion.y - 4, tamano.x + 8, tamano.y + 28), "MAPA DE INFLUENCIA");

        // Heatmap (Y invertida porque GUI tiene origen arriba-izq y nuestro grid abajo-izq)
        Rect r = new Rect(posicion.x, posicion.y + 18, tamano.x, tamano.y);
        GUI.DrawTextureWithTexCoords(r, textura, new Rect(0, 1, 1, -1));

        // Texto de ayuda
        GUI.Label(new Rect(posicion.x, posicion.y + tamano.y + 18, tamano.x, 16), $"M: ocultar | Rojo vs Azul");
    }
}
