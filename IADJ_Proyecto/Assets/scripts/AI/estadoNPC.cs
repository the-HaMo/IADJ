using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private float alturaIcono = 3.0f;
    [SerializeField] private Vector3 offsetIcono = Vector3.zero;
    [SerializeField] private float escalaLocalIcono = 0.1397475f;
    [SerializeField] private int ordenRenderIcono = 50;

    [Header("Imagenes de estado (Assets/Materials)")]
    [SerializeField] private Texture2D texturaEspada;
    [SerializeField] private Texture2D texturaEscudo;
    [SerializeField] private Texture2D texturaOjo;

    private GameObject iconoActual;
    private Transform iconoTransform;
    private Sprite spriteAtaque;
    private Sprite spriteDefensa;
    private Sprite spriteVigilancia;
    private NPCPatrol patrol;

    private void Awake()
    {
        patrol = GetComponent<NPCPatrol>();
        CargarTexturasIconoEnEditorSiFaltan();
    }

    private void Start()
    {
        // Se respeta el estado inicial asignado por el spawner (Vigilancia, Ataque, Defensa).
        // Ya no sobrescribimos con el EstadoTacticoGlobal al nacer.
        
        if (EstadoTacticoGlobal.GuerraTotalActiva)
        {
            estadoPrevioGuerraTotal = estadoActual;
            estadoActual = EstadoNPC.Ataque;
            enGuerraTotal = true;
        }

        CrearIconoEstado();
        AplicarComportamientoDeEstado();
    }

    private void OnEnable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado += AlCambiarEstadoGlobal;
    }

    private void OnDisable()
    {
        EstadoTacticoGlobal.OnEstadoCambiado -= AlCambiarEstadoGlobal;
    }

    private EstadoNPC estadoPrevioGuerraTotal;
    private bool enGuerraTotal = false;

    private void AlCambiarEstadoGlobal()
    {
        if (EstadoTacticoGlobal.GuerraTotalActiva)
        {
            if (!enGuerraTotal)
            {
                estadoPrevioGuerraTotal = estadoActual;
                enGuerraTotal = true;
                
                if (estadoActual != EstadoNPC.Ataque)
                {
                    estadoActual = EstadoNPC.Ataque;
                    CrearIconoEstado();
                    AplicarComportamientoDeEstado();
                }
            }
        }
        else
        {
            if (enGuerraTotal)
            {
                enGuerraTotal = false;
                SetEstado(estadoPrevioGuerraTotal);
            }
        }
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

        SpriteRenderer renderer = iconoActual.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            AjustarEscalaIcono(renderer);
        }

        iconoTransform.localRotation = Quaternion.identity;
        ActualizarPosicionIcono();
    }

    private void ActualizarPosicionIcono()
    {
        if (iconoTransform == null) return;

        Vector3 posicion = Vector3.up * alturaIcono + offsetIcono;
        iconoTransform.localPosition = posicion;
        iconoTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private GameObject CrearIconoPorEstado(EstadoNPC estado)
    {
        return estado switch
        {
            EstadoNPC.Ataque => CrearIconoImagen("IconoAtaque", ObtenerSpriteAtaque(), Color.white),
            EstadoNPC.Defensa => CrearIconoImagen("IconoDefensa", ObtenerSpriteDefensa(), Color.white),
            EstadoNPC.Vigilancia => CrearIconoImagen("IconoVigilancia", ObtenerSpriteVigilancia(), Color.white),
            _ => null
        };
    }

    private GameObject CrearIconoImagen(string nombre, Sprite sprite, Color tint)
    {
        GameObject go = new GameObject(nombre);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = ordenRenderIcono;

        if (sprite == null)
        {
            Debug.LogWarning($"No se pudo crear el sprite para {nombre}.", this);
        }

        return go;
    }

    private Sprite ObtenerSpriteAtaque()
    {
        if (spriteAtaque == null)
        {
            spriteAtaque = CrearSpriteDesdeTextura(texturaEspada);
        }

        return spriteAtaque;
    }

    private Sprite ObtenerSpriteDefensa()
    {
        if (spriteDefensa == null)
        {
            spriteDefensa = CrearSpriteDesdeTextura(texturaEscudo);
        }

        return spriteDefensa;
    }

    private Sprite ObtenerSpriteVigilancia()
    {
        if (spriteVigilancia == null)
        {
            spriteVigilancia = CrearSpriteDesdeTextura(texturaOjo);
        }

        return spriteVigilancia;
    }

    private Sprite CrearSpriteDesdeTextura(Texture2D textura)
    {
        if (textura == null) return null;

        Rect recorte = ObtenerRectRecorte(textura);
        return Sprite.Create(textura, recorte, new Vector2(0.5f, 0.5f), 100f);
    }

    private void AjustarEscalaIcono(SpriteRenderer renderer)
    {
        renderer.transform.localScale = Vector3.one * escalaLocalIcono;
    }

    private Rect ObtenerRectRecorte(Texture2D textura)
    {
        try
        {
            Color32[] pixeles = textura.GetPixels32();
            int ancho = textura.width;
            int alto = textura.height;

            int minX = ancho;
            int minY = alto;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < alto; y++)
            {
                int fila = y * ancho;
                for (int x = 0; x < ancho; x++)
                {
                    Color32 c = pixeles[fila + x];
                    if (c.a <= 10) continue;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX >= minX && maxY >= minY)
            {
                return new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
            }
        }
        catch
        {
            // Si la textura no es legible, usamos la imagen completa.
        }

        return new Rect(0f, 0f, textura.width, textura.height);
    }

    private void CargarTexturasIconoEnEditorSiFaltan()
    {
#if UNITY_EDITOR
        if (texturaEspada == null)
            texturaEspada = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/espada.png");
        if (texturaEscudo == null)
            texturaEscudo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/escudo.png");
        if (texturaOjo == null)
            texturaOjo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/ojo.png");
#endif
    }

    // Aplica el comportamiento segun el estado con el que nace el NPC.
    // El comportamiento "fino" (patrullar, perseguir, atacar...) lo gestiona PercepcionNPC,
    // que consulta GetEstadoActual(). Aqui solo activamos/desactivamos la patrulla base.
    private void AplicarComportamientoDeEstado()
    {
        if (patrol != null)
        {
            patrol.enabled = (estadoActual == EstadoNPC.Vigilancia);
        }

        PercepcionNPC percepcion = GetComponent<PercepcionNPC>();
        if (percepcion != null)
        {
            switch (estadoActual)
            {
                case EstadoNPC.Vigilancia:
                    percepcion.PrepararVigilancia();
                    break;
                case EstadoNPC.Ataque:
                    percepcion.PrepararAtaque();
                    break;
                case EstadoNPC.Defensa:
                    percepcion.PrepararDefensa();
                    break;
            }
        }
    }

    public EstadoNPC GetEstadoActual() => estadoActual;

    /// <summary>Devuelve el estado que debe usarse para el respawn (ignora el override de Guerra Total).</summary>
    public EstadoNPC GetEstadoRespawn() => enGuerraTotal ? estadoPrevioGuerraTotal : estadoActual;

    public void SetEstado(EstadoNPC nuevo)
    {
        if (enGuerraTotal)
        {
            estadoPrevioGuerraTotal = nuevo;
            return;
        }

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
