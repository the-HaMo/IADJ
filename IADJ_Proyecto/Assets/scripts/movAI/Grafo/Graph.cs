using UnityEngine;

public class Graph : MonoBehaviour
{
    [Header("Configuración del Grid")]
    public int nFilas;       // Número de filas
    public int nColumnas;    // Número de columnas
    public float tamCelda; // Tamaño de cada celda
    private Vector3 origen; // Posición (0,0) del grid en el mundo
    [Header("Depuración")]
    public bool mostrarGrid = true; // Activa/desactiva la visualización de las líneas

 
    public Vector3 GetWorldPosition(int fila, int columna)
    {
        return origen + new Vector3(columna * tamCelda, 0, fila * tamCelda);
    }

    public void GetGridPosition(Vector3 worldPosition, out int fila, out int columna)
    {
        columna = Mathf.FloorToInt((worldPosition.x - origen.x) / tamCelda);
        fila = Mathf.FloorToInt((worldPosition.z - origen.z) / tamCelda);
    }

    private void OnDrawGizmos()
    {
        if (!mostrarGrid)
        {
            return;
        }

        DibujarGrid();
    }

    private bool isOcupada(int fila, int columna)
    {
        Vector3 centroCelda = GetWorldPosition(fila, columna) + new Vector3(tamCelda / 2f, 1f, tamCelda / 2f);
        Collider[] colisiones = Physics.OverlapBox(centroCelda, new Vector3(tamCelda / 2f, 2f, tamCelda / 2f));
        foreach (Collider colision in colisiones)
        {
            if (colision.CompareTag("OCUPADO"))
            {
                return true;
            }
        }
        return false;
    }

    private void DibujarGrid()
    {
        // Dibujamos las líneas del grid
        Gizmos.color = Color.white;
        for (int i = 0; i <= nFilas; i++)
        {
            Vector3 inicio = GetWorldPosition(i, 0);
            Vector3 fin = GetWorldPosition(i, nColumnas);
            Gizmos.DrawLine(inicio, fin);
        }

        for (int j = 0; j <= nColumnas; j++)
        {
            Vector3 inicio = GetWorldPosition(0, j);
            Vector3 fin = GetWorldPosition(nFilas, j);
            Gizmos.DrawLine(inicio, fin);
        }

        // Comprobamos cada celda y la dibujamos en rojo si está ocupada
        for (int i = 0; i < nFilas; i++)
        {
            for (int j = 0; j < nColumnas; j++)
            {
                if (isOcupada(i, j))
                {
                    DibujarOcupado(i, j);
                }
            }
        }
    }

    private void DibujarOcupado(int i, int j)
    {
        Vector3 p0 = GetWorldPosition(i, j);
        Vector3 p1 = GetWorldPosition(i, j + 1);
        Vector3 p2 = GetWorldPosition(i + 1, j + 1);
        Vector3 p3 = GetWorldPosition(i + 1, j);

        // Dibujar el contorno en rojo
        Gizmos.color = Color.red;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);

        // Añadir una cruz para identificarla mejor visualmente
        Gizmos.DrawLine(p0, p2);
        Gizmos.DrawLine(p1, p3);
    }
}

