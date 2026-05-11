using System.Collections.Generic;
using UnityEngine;

// Mapa de influencia de bandos usando el algoritmo de inundacion de Dijkstra.
//
// Diferencia con suma directa:
//   - Cada celda guarda la influencia MAS ALTA de UNA sola unidad (no la suma).
//   - Una unidad fuerte es mas importante que muchas debiles.
//   - La influencia se propaga celda a celda con decaimiento multiplicativo.
//
// Como lo usa el resto del proyecto:
//  - Aestrella.cs lee la influencia ENEMIGA como coste adicional (pathfinding tactico).
//  - PercepcionNPC consulta la influencia para toma de decisiones tacticas.
//  - MinimapaInfluencia dibuja los mapas como heatmap (req. e).

public class MapaInfluencia : MonoBehaviour
{
    public static MapaInfluencia Instance { get; private set; }

    [Header("Configuracion general")]
    [Tooltip("Cada cuantos segundos se recalcula todo el mapa.")]
    public float intervaloActualizacion = 0.5f;

    [Tooltip("Multiplicador del poder del NPC al traducir a influencia inicial.")]
    public float pesoPoder = 0.3f;

    [Header("Dijkstra Flood")]
    [Tooltip("Factor de decaimiento multiplicativo por celda (0-1). 0.85 = pierde 15% por paso.")]
    [Range(0.5f, 0.99f)]
    public float decaimiento = 0.85f;

    [Tooltip("Influencia minima para seguir propagando (corte de propagacion).")]
    public float umbralMinimo = 0.05f;

    private GridManager gridManager;
    private float[,] infRojo;
    private float[,] infAzul;
    private float nextUpdate;
    private int sizeX, sizeY;

  // Estructuras para el heap de Dijkstra (max-heap por influencia)
    private struct EntradaHeap
    {
        public float inf;
        public int x, y;
    }

    private class MaxHeap
    {
        private readonly List<EntradaHeap> datos = new List<EntradaHeap>();

        public int Count => datos.Count;

        public void Clear() => datos.Clear();

        public void Push(EntradaHeap e)
        {
            datos.Add(e);
            SubirUltimo();
        }

        public EntradaHeap Pop()
        {
            EntradaHeap top = datos[0];
            int ultimo = datos.Count - 1;
            datos[0] = datos[ultimo];
            datos.RemoveAt(ultimo);
            if (datos.Count > 0) BajarPrimero();
            return top;
        }

        private void SubirUltimo()
        {
            int i = datos.Count - 1;
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (datos[i].inf > datos[padre].inf)
                {
                    EntradaHeap tmp = datos[i]; datos[i] = datos[padre]; datos[padre] = tmp;
                    i = padre;
                }
                else break;
            }
        }

        private void BajarPrimero()
        {
            int i = 0, n = datos.Count;
            while (true)
            {
                int mayor = i;
                int l = 2 * i + 1, r = 2 * i + 2;
                if (l < n && datos[l].inf > datos[mayor].inf) mayor = l;
                if (r < n && datos[r].inf > datos[mayor].inf) mayor = r;
                if (mayor == i) break;
                EntradaHeap tmp = datos[i]; datos[i] = datos[mayor]; datos[mayor] = tmp;
                i = mayor;
            }
        }
    }

    private readonly MaxHeap heap = new MaxHeap();

       void Awake()
    {
        Instance = this;
        gridManager = FindFirstObjectByType<GridManager>();
    }

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("[MapaInfluencia] No se encontro GridManager.");
            enabled = false;
            return;
        }

        sizeX = gridManager.GridSizeX;
        sizeY = gridManager.GridSizeY;
        infRojo = new float[sizeX, sizeY];
        infAzul = new float[sizeX, sizeY];
        Recalcular();
    }

    void Update()
    {
        if (Time.time >= nextUpdate)
        {
            Recalcular();
            nextUpdate = Time.time + intervaloActualizacion;
        }
    }


    public void Recalcular()
    {
        if (gridManager == null || infRojo == null) return;

        System.Array.Clear(infRojo, 0, infRojo.Length);
        System.Array.Clear(infAzul, 0, infAzul.Length);

        NPCStats[] todos = FindObjectsByType<NPCStats>(FindObjectsSortMode.None);

        InundarDijkstra(todos, Bando.Rojo, infRojo);
        InundarDijkstra(todos, Bando.Azul, infAzul);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Node n = gridManager.GetNode(x, y);
                if (n == null) continue;
                n.influenceValue = Mathf.RoundToInt(infRojo[x, y] + infAzul[x, y]);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Inundacion de Dijkstra para un bando.
    //
    // Algoritmo:
    //   1. Sembrar el heap con todas las unidades del bando (fuerza = poder * pesoPoder).
    //   2. Extraer la celda de mayor influencia.
    //   3. Para cada vecino: nuevaInf = infActual * decaimiento.
    //      Si nuevaInf supera el valor actual del vecino, actualizar y encolar.
    //   4. Repetir hasta vaciar el heap o quedar por debajo del umbral.
    //
    // Resultado: cada celda contiene la influencia MAS ALTA de UNA unidad
    // (no la suma), lo que hace que unidades fuertes sean mas relevantes.
    // -----------------------------------------------------------------------
    private void InundarDijkstra(NPCStats[] todos, Bando bando, float[,] mapa)
    {
        heap.Clear();
        foreach (NPCStats npc in todos)
        {
            if (npc == null || npc.miBando != bando) continue;

            Node n = npc.ObtenerNodoActual();
            if (n == null) continue;

            float fuerza = npc.poder * pesoPoder;
            int gx = n.gridX, gy = n.gridY;

            if (fuerza > mapa[gx, gy])
            {
                mapa[gx, gy] = fuerza;
                heap.Push(new EntradaHeap { inf = fuerza, x = gx, y = gy });
            }
        }

        // Decaimiento diagonal: un paso diagonal recorre sqrt(2) veces mas distancia,
        // por lo que aplicamos decaimiento^sqrt(2) para obtener forma circular.
        float decaimientoDiagonal = Mathf.Pow(decaimiento, 1.414f);

        // --- Propagacion Dijkstra ---
        while (heap.Count > 0)
        {
            EntradaHeap actual = heap.Pop();
            int cx = actual.x, cy = actual.y;

            // Entrada obsoleta: otra ruta ya llego con mayor influencia
            if (mapa[cx, cy] > actual.inf + 0.001f) continue;

            // Propagar a los 8 vecinos
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= sizeX || ny < 0 || ny >= sizeY) continue;

                    // Paso diagonal = sqrt(2) veces mas largo -> mayor decaimiento
                    bool esDiagonal = dx != 0 && dy != 0;
                    float nuevaInf = actual.inf * (esDiagonal ? decaimientoDiagonal : decaimiento);

                    if (nuevaInf < umbralMinimo) continue;

                    if (nuevaInf > mapa[nx, ny])
                    {
                        mapa[nx, ny] = nuevaInf;
                        heap.Push(new EntradaHeap { inf = nuevaInf, x = nx, y = ny });
                    }
                }
            }
        }
    }


    public float GetInfluencia(Bando bando, int x, int y)
    {
        if (!EnRango(x, y)) return 0f;
        return bando == Bando.Rojo ? infRojo[x, y] : infAzul[x, y];
    }

    public float GetInfluenciaEnemiga(Bando miBando, int x, int y)
    {
        if (!EnRango(x, y)) return 0f;
        return miBando == Bando.Rojo ? infAzul[x, y] : infRojo[x, y];
    }

    public float GetInfluenciaPropia(Bando miBando, int x, int y)
    {
        return GetInfluencia(miBando, x, y);
    }

    // Influencia neta: positivo = Rojo domina, negativo = Azul domina
    public float GetInfluenciaNeta(int x, int y)
    {
        if (!EnRango(x, y)) return 0f;
        return infRojo[x, y] - infAzul[x, y];
    }

    // Tension = suma de ambas influencias (actividad total)
    public float GetTension(int x, int y)
    {
        if (!EnRango(x, y)) return 0f;
        return infRojo[x, y] + infAzul[x, y];
    }

    // Vulnerabilidad = zonas disputadas: 2 * min(rojo, azul)
    public float GetVulnerabilidad(int x, int y)
    {
        if (!EnRango(x, y)) return 0f;
        float r = infRojo[x, y];
        float a = infAzul[x, y];
        return (r + a) - Mathf.Abs(r - a);
    }

    public float GetInfluenciaEnemigaEnMundo(Bando miBando, Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        Node n = gridManager.NodeFromWorldPoint(worldPos);
        if (n == null) return 0f;
        return GetInfluenciaEnemiga(miBando, n.gridX, n.gridY);
    }

    public float GetInfluenciaPropiaEnMundo(Bando miBando, Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        Node n = gridManager.NodeFromWorldPoint(worldPos);
        if (n == null) return 0f;
        return GetInfluenciaPropia(miBando, n.gridX, n.gridY);
    }

    private bool EnRango(int x, int y)
    {
        return infRojo != null && x >= 0 && x < sizeX && y >= 0 && y < sizeY;
    }
}
