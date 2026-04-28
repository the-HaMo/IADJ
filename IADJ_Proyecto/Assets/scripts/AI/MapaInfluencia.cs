using System.Collections.Generic;
using UnityEngine;

// Mapa de influencia de bandos. Cada NPC vivo "irradia" influencia en torno a su
// posicion, decreciendo con la distancia. Hay un mapa por bando.
//
// Como lo usa el resto del proyecto:
//  - El A* (Aestrella.cs) lee la influencia ENEMIGA al planear caminos para
//    evitar zonas peligrosas (pathfinding tactico, requisito i).
//  - PercepcionNPC consulta la influencia para decidir huir si esta en zona
//    enemiga con poca vida (requisito f).
//  - MinimapaInfluencia dibuja los dos mapas como un heatmap (requisito e).
public class MapaInfluencia : MonoBehaviour
{
    public static MapaInfluencia Instance { get; private set; }

    [Header("Configuracion")]
    [Tooltip("Cada cuantos segundos se recalcula todo el mapa.")]
    public float intervaloActualizacion = 0.5f;

    [Tooltip("Radio en celdas que cada NPC irradia.")]
    public int radioInfluencia = 10;

    [Tooltip("Multiplicador del poder del NPC al traducir a influencia bruta.")]
    public float pesoPoder = 0.3f;

    [Tooltip("Si esta activo, el A* sumara la influencia enemiga al coste de paso.")]
    public bool pathfindingTacticoActivo = true;

    [Tooltip("Cuanto pesa la influencia enemiga en el coste del A* (0 desactiva).")]
    public float pesoCostePathfinding = 1.5f;

    private GridManager gridManager;
    private float[,] infRojo;
    private float[,] infAzul;
    private float nextUpdate;
    private int sizeX, sizeY;

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

        // 1. Limpiar mapas
        System.Array.Clear(infRojo, 0, infRojo.Length);
        System.Array.Clear(infAzul, 0, infAzul.Length);

        // 2. Cada NPC vivo irradia influencia en su entorno
        NPCStats[] todos = FindObjectsByType<NPCStats>(FindObjectsSortMode.None);
        foreach (NPCStats npc in todos)
        {
            if (npc == null || npc.miBando == Bando.Default) continue;

            Node n = npc.ObtenerNodoActual();
            if (n == null) continue;

            float fuerza = npc.poder * pesoPoder;
            EsparcirInfluencia(n.gridX, n.gridY, fuerza, npc.miBando);
        }

        // 3. Volcar al Node.influenceValue para los NPCs que no usan MapaInfluencia
        //    directamente (compatibilidad con Aestrella sin pasar bando).
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Node n = gridManager.GetNode(x, y);
                if (n == null) continue;
                // Influencia "neta" como valor positivo (bandera de zona disputada)
                n.influenceValue = Mathf.RoundToInt(infRojo[x, y] + infAzul[x, y]);
            }
        }
    }

    private void EsparcirInfluencia(int cx, int cy, float fuerza, Bando bando)
    {
        int rad = radioInfluencia;
        int x0 = Mathf.Max(0, cx - rad);
        int x1 = Mathf.Min(sizeX - 1, cx + rad);
        int y0 = Mathf.Max(0, cy - rad);
        int y1 = Mathf.Min(sizeY - 1, cy + rad);

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int distSq = dx * dx + dy * dy;
                if (distSq > rad * rad) continue;

                // Cae con 1/(1+d^2) -> intensa cerca, suave lejos
                float aporte = fuerza / (1f + distSq);

                if (bando == Bando.Rojo) infRojo[x, y] += aporte;
                else if (bando == Bando.Azul) infAzul[x, y] += aporte;
            }
        }
    }

    // ------- API publica -------

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

    // Devuelve coste de paso para pathfinding tactico. El NPC del bando "miBando"
    // pagara mas por atravesar zonas con mucha influencia enemiga.
    public int GetCosteInfluencia(Bando miBando, int x, int y)
    {
        if (!pathfindingTacticoActivo) return 0;
        float enem = GetInfluenciaEnemiga(miBando, x, y);
        return Mathf.RoundToInt(enem * pesoCostePathfinding);
    }

    // En posicion del mundo: util para PercepcionNPC.
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

    // Toggle del componente tactico del pathfinding (req. k del enunciado)
    void Update_Toggle()
    {
        // Llamado desde Update via teclado en otro script si se quiere
    }
}
