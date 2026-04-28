using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EstadoNPC
{
    Vigilancia,
    Ataque,
    Defensa
}

public class estadoNPC : MonoBehaviour
{
    [Header("Estado del NPC")]
    [SerializeField] private EstadoNPC estadoActual = EstadoNPC.Vigilancia;

    [Header("Posicion del icono")]
    [SerializeField] private float alturaIcono = 2.5f;
    [SerializeField] private Vector3 offsetIcono = Vector3.zero;
    [SerializeField] private float escalaIcono = 1f;

    private GameObject iconoActual;
    private Transform iconoTransform;
    private NPCPatrol patrol;

    private void Awake()
    {
        patrol = GetComponent<NPCPatrol>();
    }

    private void Start()
    {
        CrearIconoEstado();
        AplicarComportamientoDeEstado();
    }

    private void LateUpdate()
    {
        ActualizarPosicionIcono();
    }

    private void CrearIconoEstado()
    {
        if (iconoActual != null)
        {
            Destroy(iconoActual);
        }

        iconoActual = CrearIconoPorEstado(estadoActual);
        if (iconoActual == null) return;

        iconoActual.transform.SetParent(transform, false);
        iconoTransform = iconoActual.transform;

        iconoTransform.localRotation = Quaternion.identity;
        iconoTransform.localScale = Vector3.one * escalaIcono;
        ActualizarPosicionIcono();
    }

    private void ActualizarPosicionIcono()
    {
        if (iconoTransform == null) return;
        iconoTransform.localPosition = Vector3.up * alturaIcono + offsetIcono;
    }

    private GameObject CrearIconoPorEstado(EstadoNPC estado)
    {
        return estado switch
        {
            EstadoNPC.Ataque => CrearPrimitiva(PrimitiveType.Cube, "IconoAtaque", Color.red),
            EstadoNPC.Defensa => CrearPrimitiva(PrimitiveType.Sphere, "IconoDefensa", Color.blue),
            EstadoNPC.Vigilancia => CrearPiramide("IconoVigilancia", Color.yellow),
            _ => null
        };
    }

    private GameObject CrearPrimitiva(PrimitiveType tipo, string nombre, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }

        return go;
    }

    private GameObject CrearPiramide(string nombre, Color color)
    {
        GameObject go = new GameObject(nombre);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        MeshCollider mc = go.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.name = "PiramideMesh";

        Vector3[] vertices =
        {
            new Vector3(0f, 0.5f, 0f),      // 0: punta
            new Vector3(-0.5f, -0.5f, -0.5f), // 1: base
            new Vector3(0.5f, -0.5f, -0.5f),  // 2
            new Vector3(0.5f, -0.5f, 0.5f),   // 3
            new Vector3(-0.5f, -0.5f, 0.5f)   // 4
        };

        int[] triangles =
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 1,
            1, 3, 2,
            1, 4, 3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        mc.sharedMesh = mesh;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mr.material = mat;

        return go;
    }

    // Aplica el comportamiento segun el estado con el que nace el NPC.
    // El comportamiento "fino" (patrullar, perseguir, atacar...) lo gestiona PercepcionNPC,
    // que consulta GetEstadoActual(). Aqui solo activamos/desactivamos la patrulla base.
    private void AplicarComportamientoDeEstado()
    {
        // Vigilancia: la patrulla esta activa.
        // Ataque: la patrulla NO esta activa (PercepcionNPC busca enemigos con radio mayor).
        // Defensa: la patrulla NO esta activa (PercepcionNPC se queda anclado a la base).
        if (patrol != null)
        {
            patrol.enabled = (estadoActual == EstadoNPC.Vigilancia);
        }
    }

    public EstadoNPC GetEstadoActual()
    {
        return estadoActual;
    }

    public void SetEstado(EstadoNPC nuevo)
    {
        if (estadoActual == nuevo) return;
        estadoActual = nuevo;
        CrearIconoEstado();
        AplicarComportamientoDeEstado();
    }

    // Helpers para que PercepcionNPC consulte semanticamente
    public bool EsAgresivo()  => estadoActual == EstadoNPC.Ataque;
    public bool EsDefensivo() => estadoActual == EstadoNPC.Defensa;
    public bool EsVigilante() => estadoActual == EstadoNPC.Vigilancia;
}
