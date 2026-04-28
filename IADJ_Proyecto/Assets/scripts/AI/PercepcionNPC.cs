using UnityEngine;

public class PercepcionNPC : MonoBehaviour
{
    [Header("Detección")]
    public LayerMask capaNPC;
    public float intervaloBusqueda = 0.2f;

    [Header("Coordenadas Hospitales (fallback si no hay WayPoints)")]
    public Vector3 hospitalRojo;
    public Vector3 hospitalAzul;

    [Header("Decisiones tacticas (mapa de influencia)")]
    [Tooltip("Influencia enemiga minima para considerar la zona insegura.")]
    public float umbralInfluenciaEnemiga = 3f;
    [Tooltip("En modo GUERRA TOTAL multiplica el radio de percepcion.")]
    public float multRadioGuerraTotal = 2.5f;
    [Tooltip("En estado individual ATAQUE, multiplica el radio de percepcion.")]
    public float multRadioEstadoAtaque = 1.6f;
    [Tooltip("En estado individual DEFENSA, distancia maxima a la base antes de volver.")]
    public float distanciaMaxAlAnclaDefensa = 12f;

    private NPCStats stats;
    private PathFollowing path;
    private Pathfinding pathfinder;
    private AgentNPC agent;
    private NPCPatrol patrol;
    private estadoNPC estado;
    private MapaInfluencia mapa;
    private WayPoints waypoints;

    private TacticasPorTipo.PerfilTactico perfil;

    private Transform enemigoActual;
    private Vector3 lastDest;
    private Vector3 anclaDefensa; // posicion inicial al entrar en estado Defensa
    private float nextTick;
    private float nextAtaque;

    private static bool mostrarGizmosGlobal = false;

    void Awake()
    {
        stats = GetComponent<NPCStats>();
        path = GetComponent<PathFollowing>();
        pathfinder = FindFirstObjectByType<Pathfinding>();
        agent = GetComponent<AgentNPC>();
        patrol = GetComponent<NPCPatrol>();
        estado = GetComponent<estadoNPC>();
        mapa = FindFirstObjectByType<MapaInfluencia>();
        waypoints = FindFirstObjectByType<WayPoints>();
    }

    void Start()
    {
        // Cargar el perfil tactico del tipo de unidad (req. a)
        perfil = TacticasPorTipo.GetPerfil(stats.tipoUnidad);
        anclaDefensa = transform.position;
    }

    void OnEnable()
    {
        if (stats == null) stats = GetComponent<NPCStats>();
        if (stats != null) stats.OnAtacado += AlSerAtacado;
    }

    void OnDisable()
    {
        if (stats != null) stats.OnAtacado -= AlSerAtacado;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) mostrarGizmosGlobal = !mostrarGizmosGlobal;

        // --- LECTURA DE CONTEXTO ---
        ModoEstrategico modo = (EstrategiaBando.Instance != null)
            ? EstrategiaBando.Instance.GetModo(stats.miBando)
            : ModoEstrategico.Defensivo;

        EstadoNPC estadoIndividual = (estado != null) ? estado.GetEstadoActual() : EstadoNPC.Vigilancia;

        // 1. Curacion (PRIORIDAD MAXIMA)
        bool huirPorInfluencia = DebeHuirPorInfluencia();
        if (stats.NecesitaCuracion() || huirPorInfluencia)
        {
            enemigoActual = null;
            ActualizarRuta(ObtenerPosicionHospital(), 1f);
            return;
        }

        // 2. Calcular radio de percepcion segun perfil + estado + modo
        float radio = CalcularRadioPercepcion(modo, estadoIndividual);

        // 3. Busqueda de enemigos (cada intervalo)
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + intervaloBusqueda;
            enemigoActual = BuscarEnemigoCercano(radio);
        }

        // 4. Comportamiento
        if (enemigoActual != null)
        {
            if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }

            float dist = Vector3.Distance(transform.position, enemigoActual.position);
            // Distancia preferida segun el tipo (arquero/explorador kitean, melee se planta)
            float distPref = stats.rangoAtaque * perfil.distanciaCombatePref;

            // Si esta dentro de la "zona buena" (entre rangoAtaque y distPref), parar y atacar
            if (dist <= stats.rangoAtaque)
            {
                Parar();
                EjecutarAtaque();
            }
            else
            {
                Vector3 destino = CalcularPosicionPersecucion(distPref);
                ActualizarRuta(destino, 1f);
            }
        }
        else
        {
            GestionarSinEnemigos(modo, estadoIndividual);
        }
    }

    // El radio de percepcion depende de:
    //  - Modo del bando (GuerraTotal x2.5)
    //  - Estado individual (Ataque x1.6, Defensa estandar)
    //  - Perfil del tipo (bonus si esta en su bioma preferido)
    private float CalcularRadioPercepcion(ModoEstrategico modo, EstadoNPC estadoInd)
    {
        float radio = stats.radioPercepcion;

        if (modo == ModoEstrategico.GuerraTotal)
            radio *= multRadioGuerraTotal;
        else if (estadoInd == EstadoNPC.Ataque)
            radio *= multRadioEstadoAtaque;

        // Bonus por terreno preferido
        if (stats.ObtenerBiomaActual() == perfil.biomaPreferido)
            radio *= perfil.bonusAgresividadEnPreferido;

        return radio;
    }

    // Posicion a la que ir para combatir respetando la distancia preferida del tipo.
    // Arqueros/exploradores no se acercan, solo van hasta su rango ideal.
    private Vector3 CalcularPosicionPersecucion(float distanciaPref)
    {
        Vector3 desdeEnemigo = transform.position - enemigoActual.position;
        // Fix: si yo y enemigo estan exactamente en la misma posicion, usar forward
        if (desdeEnemigo.sqrMagnitude < 0.0001f)
        {
            desdeEnemigo = transform.forward;
        }
        Vector3 dir = desdeEnemigo.normalized;
        return enemigoActual.position + dir * distanciaPref;
    }

    // Si vida % bajo el umbral del tipo Y la zona tiene mucha influencia enemiga -> huir.
    // Los tanques no huyen por influencia (perfil.aguantaInfluenciaEnemiga).
    private bool DebeHuirPorInfluencia()
    {
        if (mapa == null) return false;
        if (perfil.aguantaInfluenciaEnemiga) return false;
        if (stats.VidaPorcentaje > perfil.vidaCriticaPct) return false;

        float infEnem = mapa.GetInfluenciaEnemigaEnMundo(stats.miBando, transform.position);
        return infEnem >= umbralInfluenciaEnemiga;
    }

    private Vector3 ObtenerPosicionHospital()
    {
        if (waypoints != null) return waypoints.GetCuracion(stats.miBando);
        return (stats.miBando == Bando.Rojo) ? hospitalRojo : hospitalAzul;
    }

    private void GestionarSinEnemigos(ModoEstrategico modo, EstadoNPC estadoInd)
    {
        // Estado individual DEFENSA tiene prioridad sobre modo del bando:
        // si el NPC fue puesto en Defensa, se queda anclado a su zona.
        if (estadoInd == EstadoNPC.Defensa)
        {
            float distAncla = Vector3.Distance(transform.position, anclaDefensa);
            if (distAncla > distanciaMaxAlAnclaDefensa)
            {
                ActualizarRuta(anclaDefensa, 1f);
            }
            else
            {
                Parar();
            }
            return;
        }

        // Estado individual ATAQUE: avanzar hacia base enemiga aunque el bando este Defensivo
        if (estadoInd == EstadoNPC.Ataque)
        {
            IrABaseEnemiga();
            return;
        }

        // Estado Vigilancia: depende del modo del bando
        switch (modo)
        {
            case ModoEstrategico.Defensivo:
                GestionarVigilancia();
                break;

            case ModoEstrategico.Ofensivo:
            case ModoEstrategico.GuerraTotal:
                IrABaseEnemiga();
                break;
        }
    }

    private void IrABaseEnemiga()
    {
        if (waypoints == null) { GestionarVigilancia(); return; }

        Bando enemigo = (stats.miBando == Bando.Rojo) ? Bando.Azul : Bando.Rojo;
        Vector3 baseEnemiga = waypoints.GetBase(enemigo);
        ActualizarRuta(baseEnemiga, 3f);
        if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }
    }

    private void EjecutarAtaque()
    {
        if (Time.time >= nextAtaque && enemigoActual != null)
        {
            NPCStats targetStats = enemigoActual.GetComponent<NPCStats>();
            if (targetStats != null)
            {
                SistemaCombate.ResultadoAtaque res = targetStats.RecibirAtaqueDe(stats);
                nextAtaque = Time.time + stats.velAtaque;

                if (res.esCritico)
                    Debug.Log($"<color=orange>CRITICO</color> {stats.name} -> {targetStats.name}: {res.danio:F0}");
                else
                    Debug.Log($"{stats.name} ({stats.tipoUnidad}) -> {targetStats.name} ({targetStats.tipoUnidad}): FA={res.fa:F0} FD={res.fd:F0} dano={res.danio:F0}");
            }
        }
    }

    // Cuando el NPC recibe un ataque: si no tenia objetivo o el atacante es mas
    // peligroso (peor matchup FAD/FTA/FTD), pasa a apuntarle. Tambien aplica un
    // bonus si el atacante esta en su lista de objetivos preferidos por tipo.
    private void AlSerAtacado(NPCStats atacante)
    {
        if (atacante == null) return;
        if (atacante.miBando == stats.miBando) return;
        if (stats.NecesitaCuracion()) return;

        if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }

        if (enemigoActual == null)
        {
            enemigoActual = atacante.transform;
            return;
        }

        NPCStats objStats = enemigoActual.GetComponent<NPCStats>();
        if (objStats == null) { enemigoActual = atacante.transform; return; }

        Bioma terrenoYo = stats.ObtenerBiomaActual();
        float ratioAtacante = SistemaCombate.CalcularFA(atacante, atacante.ObtenerBiomaActual(), stats)
                            / Mathf.Max(1f, SistemaCombate.CalcularFD(stats, terrenoYo));
        float ratioObjActual = SistemaCombate.CalcularFA(objStats, objStats.ObtenerBiomaActual(), stats)
                             / Mathf.Max(1f, SistemaCombate.CalcularFD(stats, terrenoYo));

        // Bonus si el atacante es objetivo preferido de mi tipo (ej: lancero prioriza tanques)
        if (TacticasPorTipo.EsObjetivoPreferido(perfil, atacante.tipoUnidad))
            ratioAtacante *= 1.3f;
        if (TacticasPorTipo.EsObjetivoPreferido(perfil, objStats.tipoUnidad))
            ratioObjActual *= 1.3f;

        if (ratioAtacante > ratioObjActual)
        {
            enemigoActual = atacante.transform;
        }
    }

    private void GestionarVigilancia()
    {
        if (patrol != null && !patrol.enabled && estado != null && estado.GetEstadoActual() == EstadoNPC.Vigilancia)
            patrol.enabled = true;
        else if (patrol == null)
            Parar();
    }

    private Transform BuscarEnemigoCercano() => BuscarEnemigoCercano(stats.radioPercepcion);

    // Selecciona enemigo: el mas cercano por defecto, pero los del tipo preferido
    // tienen un descuento (3.0/dist mayor que 1/dist)
    private Transform BuscarEnemigoCercano(float radio)
    {
        if (stats.miBando == Bando.Default) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, radio, capaNPC);
        Transform mejor = null;
        float mejorScore = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            NPCStats ts = hit.GetComponent<NPCStats>();
            if (ts == null || ts.miBando == stats.miBando || ts.miBando == Bando.Default) continue;

            float d = Vector3.Distance(transform.position, hit.transform.position);
            // Score: distancia / (1 si normal, 2.0 si objetivo preferido) -> menor score = mejor
            float score = TacticasPorTipo.EsObjetivoPreferido(perfil, ts.tipoUnidad) ? d / 2.0f : d;
            if (score < mejorScore) { mejorScore = score; mejor = hit.transform; }
        }
        return mejor;
    }

    private void ActualizarRuta(Vector3 destino, float umbral)
    {
        if (Vector3.Distance(lastDest, destino) > umbral)
        {
            lastDest = destino;
            var camino = pathfinder.FindPath(transform.position, destino, stats);
            if (camino != null) path.SetPath(camino);
        }
    }

    private void Parar()
    {
        if (lastDest != Vector3.zero)
        {
            path.FinalizarMovimiento(agent);
            lastDest = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (!mostrarGizmosGlobal || stats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.radioPercepcion);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.rangoAtaque);
    }
}
