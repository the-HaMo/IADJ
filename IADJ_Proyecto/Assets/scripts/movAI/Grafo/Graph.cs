using UnityEngine;

/// <summary>
/// Clase que gestiona una rejilla (Grid) sobre el terreno en el plano ZX.
/// </summary>
public class Graph : MonoBehaviour
{
    [Header("Configuración del Grid")]
    public int nFilas = 10;       // Número de filas
    public int nColumnas = 10;    // Número de columnas
    public float tamCelda = 1.0f; // Tamaño de cada celda
    public Vector3 origen = Vector3.zero; // Posición (0,0) del grid en el mundo

    [Header("Depuración")]
    public bool mostrarGrid = true; // Activa/desactiva la visualización de las líneas

    /// <summary>
    /// Devuelve la posición en el mundo (x, z) de una celda dada su posición (i, j) en el grid.
    /// Se calcula respecto a la esquina inferior izquierda de la celda.
    /// </summary>
    public Vector3 GetWorldPosition(int fila, int columna)
    {
        return origen + new Vector3(columna * tamCelda, 0, fila * tamCelda);
    }

    /// <summary>
    /// Devuelve las coordenadas (fila, columna) del grid dada una posición en el mundo.
    /// </summary>
    public void GetGridPosition(Vector3 worldPosition, out int fila, out int columna)
    {
        columna = Mathf.FloorToInt((worldPosition.x - origen.x) / tamCelda);
        fila = Mathf.FloorToInt((worldPosition.z - origen.z) / tamCelda);
    }

    void Update()
    {
        // En cada frame, si está activada la depuración, dibujamos el grid
        if (mostrarGrid)
        {
            DibujarGrid();
        }
    }

    /// <summary>
    /// Dibuja las líneas del grid usando Debug.DrawLine.
    /// </summary>
    private void DibujarGrid()
    {
        // Dibujamos las líneas horizontales (filas)
        for (int i = 0; i <= nFilas; i++)
        {
            Vector3 inicio = GetWorldPosition(i, 0);
            Vector3 fin = GetWorldPosition(i, nColumnas);
            Debug.DrawLine(inicio, fin, Color.white);
        }

        // Dibujamos las líneas verticales (columnas)
        for (int j = 0; j <= nColumnas; j++)
        {
            Vector3 inicio = GetWorldPosition(0, j);
            Vector3 fin = GetWorldPosition(nFilas, j);
            Debug.DrawLine(inicio, fin, Color.white);
        }
    }
}

