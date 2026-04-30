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
    [Tooltip("En estado individual ATAQUE, multiplica el radio de percepcion.")]
    public float multRadioEstadoAtaque = 1.6f;

    private NPCStats stats;
    private PathFollowing path;
    private Pathfinding pathfinder;
    private AgentNPC agent;
    private NPCPatrol patrol;
    private estadoNPC estado;
    private MapaInfluencia mapa;
    private WayPoints waypoints;

    private Transform enemigoActual;
    private Vector3 lastDest;
    private float nextTick;
    private float nextAtaque;

    private bool tieneOrdenManual = false;
    private Vector3 destinoManual;


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
    }

    void OnEnable()
    {
        if (stats == null) stats = GetComponent<NPCStats>();
        if (stats != null) stats.OnAtacado += AlSerAtacado;
        EstadoTacticoGlobal.OnEstadoCambiado += ForzarRecalculoRuta;
    }

    void OnDisable()
    {
        if (stats != null) stats.OnAtacado -= AlSerAtacado;
        EstadoTacticoGlobal.OnEstadoCambiado -= ForzarRecalculoRuta;
    }

    private void ForzarRecalculoRuta()
    {
        // Al resetear lastDest, el siguiente Update detectará que "ha cambiado" y pedirá un nuevo A*
        lastDest = Vector3.zero;

        // Si la unidad tiene una orden manual del jugador, forzamos que pida el nuevo camino A* táctico
        if (tieneOrdenManual)
        {
            AsignarOrdenManual(destinoManual);
        }
    }

    void Update()
    {
        // Debug controlado globalmente por la tecla B

        if (tieneOrdenManual)
        {
            if (Vector3.Distance(transform.position, destinoManual) <= 1.5f || (path != null && !path.enabled))
            {
                tieneOrdenManual = false;
            }
            else
            {
                return;
            }
        }

        EstadoNPC estadoIndividual = (estado != null) ? estado.GetEstadoActual() : EstadoNPC.Vigilancia;

        // 1. Curacion (PRIORIDAD MAXIMA)
        // EXCEPCIÓN: Los tanques en modo Ataque son unidades de asedio suicida, no retroceden a curarse.
        bool esTanqueAtacando = (stats.tipoUnidad == TipoUnidad.Tanque && estadoIndividual == EstadoNPC.Ataque);

        if (stats.NecesitaCuracion() && !esTanqueAtacando)
        {
            enemigoActual = null;
            ActualizarRuta(ObtenerPosicionHospital(), 1f);
            return;
        }

        // 2. Calcular radio de percepcion segun perfil + estado
        float radio = CalcularRadioPercepcion(estadoIndividual);

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

            // --- Modificadores de Combate en Defensa ---
            if (estadoIndividual == EstadoNPC.Defensa)
            {
                if (stats.tipoUnidad == TipoUnidad.Arquero)
                {
                    // Mantenerse a distancia maxima (Kite)
                    if (dist < stats.rangoAtaque * 0.9f)
                    {
                        Vector3 huida = transform.position + (transform.position - enemigoActual.position).normalized * 3f;
                        ActualizarRuta(huida, 0.5f);
                        return; // Huye, no ataca
                    }
                }
                else if (stats.tipoUnidad == TipoUnidad.Explorador)
                {
                    // REFUGIO EN BIOMA: Huye al Bosque donde recibe menos daño (FTD Bosque/Explorador = 0.75)
                    if (stats.ObtenerBiomaActual() != Bioma.Bosque)
                    {
                        Vector3 destinoBosque = BuscarBiomaCercano(Bioma.Bosque);
                        if (Vector3.Distance(transform.position, destinoBosque) > 1f)
                        {
                            ActualizarRuta(destinoBosque, 1f);
                            return; // Se mueve al Bosque en vez de combatir directamente
                        }
                    }
                }
                else if (stats.tipoUnidad == TipoUnidad.Caballero)
                {
                    // Buscar aliados mediante mapa de influencias
                    Vector3 destinoAliados = BuscarMayorInfluenciaAliada();
                    if (Vector3.Distance(transform.position, destinoAliados) > 2f)
                    {
                        ActualizarRuta(destinoAliados, 1f);
                        return; // Se repliega hacia aliados
                    }
                }
            }

            // Flujo normal de ataque
            // Le sumamos 1.0f al rango de ataque como "tolerancia" por el radio físico de los colliders (0.5 + 0.5).
            // Así evitamos que dos personajes se queden empujándose sin poder atacarse si su rangoAtaque en el inspector es muy pequeño.
            float rangoEfectivo = stats.rangoAtaque + 1.0f;

            if (dist <= rangoEfectivo)
            {
                Parar();
                EjecutarAtaque();
            }
            else
            {
                // ANTI-BLOQUEO: Si nuestro objetivo está lejos, pero estamos chocando con OTRO enemigo,
                // le atacamos a él en lugar de quedarnos atascados empujando a la montonera.
                Transform enemigoBloqueando = BuscarEnemigoCercano(rangoEfectivo);
                if (enemigoBloqueando != null && enemigoBloqueando != enemigoActual)
                {
                    enemigoActual = enemigoBloqueando;
                    Parar();
                    EjecutarAtaque();
                }
                else
                {
                    // Intentar ir a una posición ligeramente más cercana que el rango máximo 
                    // para asegurar que entra en rango incluso si hay aliados empujando.
                    float distPref = Mathf.Max(0.5f, stats.rangoAtaque * 0.75f);
                    Vector3 destino = CalcularPosicionPersecucion(distPref);
                    ActualizarRuta(destino, 1f);
                }
            }
        }
        else
        {
            GestionarSinEnemigos(estadoIndividual);
        }
    }

    private float CalcularRadioPercepcion(EstadoNPC estadoInd)
    {
        float radio = stats.radioPercepcion;

        if (estadoInd == EstadoNPC.Ataque)
            radio *= multRadioEstadoAtaque;

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



    private Vector3 ObtenerPosicionHospital()
    {
        if (waypoints != null) return waypoints.GetCuracion(stats.miBando);
        return (stats.miBando == Bando.Rojo) ? hospitalRojo : hospitalAzul;
    }

    private void GestionarSinEnemigos(EstadoNPC estadoInd)
    {
        if (estadoInd == EstadoNPC.Defensa)
        {
            GestionarDefensa();
            return;
        }

        if (estadoInd == EstadoNPC.Ataque)
        {
            GestionarAtaque();
            return;
        }

        if (estadoInd == EstadoNPC.Vigilancia)
        {
            GestionarVigilancia();
        }
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

        bool esAtaque = (estado != null && estado.GetEstadoActual() == EstadoNPC.Ataque);

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            NPCStats ts = hit.GetComponent<NPCStats>();
            if (ts == null || ts.miBando == stats.miBando || ts.miBando == Bando.Default) continue;

            float score;
            if (esAtaque && stats.tipoUnidad == TipoUnidad.Explorador)
            {
                // Explorador: busca enemigo con poca vida
                score = ts.VidaActual;
            }
            else
            {
                // Por defecto: busca por distancia
                score = Vector3.Distance(transform.position, hit.transform.position);
            }

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

    public void AsignarOrdenManual(Vector3 destino)
    {
        tieneOrdenManual = true;
        destinoManual = destino;
        
        if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }
        enemigoActual = null;

        var camino = pathfinder.FindPath(transform.position, destino, stats);
        if (camino != null && camino.Count > 0)
        {
            path.SetPath(camino);
            path.enabled = true;
            if (agent != null) agent.Velocity = Vector3.zero;
        }
        else
        {
            tieneOrdenManual = false;
        }
    }

    // --- Inicializacion de Estados (llamado por estadoNPC) ---
    public void PrepararVigilancia()
    {
        enemigoActual = null; // Pierde el interes al volver a patrullar
    }

    public void PrepararAtaque()
    {
        // Listo para agredir
    }

    public void PrepararDefensa()
    {
        nextDefenseUpdate = 0f; // Fuerza a recalcular la ruta defensiva inmediatamente
    }

    // --- Utilidades Defensivas ---
    public bool TieneAliadosCerca()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.radioPercepcion, capaNPC);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            NPCStats ts = hit.GetComponent<NPCStats>();
            if (ts != null && ts.miBando == stats.miBando) return true;
        }
        return false;
    }

    private Vector3 BuscarBiomaCercano(Bioma biomaBuscado)
    {
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm == null) return transform.position;

        Vector3 mejorPos = transform.position;
        float minDist = float.MaxValue;

        for (int i = 0; i < 15; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * stats.radioPercepcion;
            rnd.y = transform.position.y;
            Node n = gm.NodeFromWorldPoint(rnd);
            if (n != null && n.bioma == biomaBuscado)
            {
                float d = Vector3.Distance(transform.position, rnd);
                if (d < minDist) { minDist = d; mejorPos = rnd; }
            }
        }
        return mejorPos;
    }

    private Vector3 BuscarMayorInfluenciaAliada()
    {
        if (mapa == null) return transform.position;
        Vector3 mejorPos = transform.position;
        float maxInf = mapa.GetInfluenciaPropiaEnMundo(stats.miBando, transform.position);

        for (int i = 0; i < 15; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * stats.radioPercepcion;
            rnd.y = transform.position.y;
            float inf = mapa.GetInfluenciaPropiaEnMundo(stats.miBando, rnd);
            if (inf > maxInf) { maxInf = inf; mejorPos = rnd; }
        }
        return mejorPos;
    }

    private float nextDefenseUpdate;
    private float nextAttackUpdate;

    private void GestionarDefensa()
    {
        if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }

        if (Time.time >= nextDefenseUpdate)
        {
            nextDefenseUpdate = Time.time + 1.5f; // Actualizar destino defensivo cada 1.5s
            Vector3 destino = CalcularDestinoDefensa();
            ActualizarRuta(destino, 2f);
        }
    }

    private Vector3 CalcularDestinoDefensa()
    {
        if (mapa == null || waypoints == null) return transform.position;

        Bando bandoPropio = stats.miBando;
        Vector3 mejorPos = transform.position;
        float mejorScore = float.MinValue;
        float radio = 20f;

        Vector3 torrePropia = waypoints.GetObjetivoMasCercano(bandoPropio, transform.position);
        // Failsafe: si está muy lejos de su área defensiva, vuelve
        if (Vector3.Distance(transform.position, torrePropia) > 35f)
            return torrePropia;

        for (int i = 0; i < 20; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * radio;
            rnd.y = transform.position.y;

            float infPropia  = mapa.GetInfluenciaPropiaEnMundo(bandoPropio, rnd);
            float infEnemiga = mapa.GetInfluenciaEnemigaEnMundo(bandoPropio, rnd);
            float distTorre  = Vector3.Distance(rnd, torrePropia);
            float score;

            switch (stats.tipoUnidad)
            {
                case TipoUnidad.Caballero:
                    // Busca la FRONTERA: donde hay influencia propia Y enemiga (línea de contacto)
                    // Se mueve hacia donde están los dos bandos, actuando como primer escudo
                    score = (infPropia + infEnemiga) - Mathf.Abs(infPropia - infEnemiga) * 2f;
                    break;

                case TipoUnidad.Lancero:
                    // Busca alta influencia propia: se agrupa donde están los aliados (formación)
                    score = infPropia * 3f - infEnemiga;
                    break;

                case TipoUnidad.Tanque:
                    // Avanza donde más enemigos hay pero dentro de su zona (influencia enemiga alta)
                    score = infEnemiga * 2f - (distTorre * 0.3f);
                    break;

                case TipoUnidad.Arquero:
                    // Zona despejada con visibilidad: baja densidad aliada, algo de presencia enemiga visible
                    score = infEnemiga * 1.5f - infPropia * 2f;
                    break;

                case TipoUnidad.Explorador:
                    // Reconocimiento: busca zona con poca influencia de ambos (terreno sin explorar / flancos)
                    score = -(infPropia + infEnemiga);
                    break;

                default:
                    score = infPropia - infEnemiga;
                    break;
            }

            if (score > mejorScore) { mejorScore = score; mejorPos = rnd; }
        }

        return mejorPos;
    }

    private void GestionarAtaque()
    {
        if (patrol != null && patrol.enabled) { patrol.enabled = false; patrol.DetenerPatrulla(); }

        if (Time.time >= nextAttackUpdate)
        {
            nextAttackUpdate = Time.time + 2.0f; 
            Vector3 destino = CalcularDestinoAtaque();
            ActualizarRuta(destino, 2.5f);
        }
    }

    private Vector3 CalcularDestinoAtaque()
    {
        if (waypoints == null || mapa == null) return transform.position;

        Bando enemigo = (stats.miBando == Bando.Rojo) ? Bando.Azul : Bando.Rojo;
        
        // Comportamiento estratégico individual en ataque:
        switch (stats.tipoUnidad)
        {
            case TipoUnidad.Tanque:
                // Va directo a torres enemigas (spawns u objetivos)
                return waypoints.GetObjetivoMasCercano(enemigo, transform.position);

            case TipoUnidad.Arquero:
                // Busca baja influencia enemiga para atacar seguro
                return BuscarDestinoPorInfluencia(enemigo, buscarBaja: true);

            default:
                // Exploradores, Caballeros, Lanceros: Estrategia Ofensiva General
                // Buscar donde hay enemigos en el mapa de influencia (Alta influencia enemiga)
                return BuscarDestinoPorInfluencia(enemigo, buscarBaja: false);
        }
    }

    private Vector3 BuscarDestinoPorInfluencia(Bando bandoEnemigo, bool buscarBaja)
    {
        Vector3 mejorPos = transform.position;
        float mejorScore = buscarBaja ? float.MaxValue : float.MinValue;
        float radioBusqueda = 25f; // Ampliamos un poco el radio de visión estratégica
        bool hayCombateCerca = false;

        for (int i = 0; i < 20; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * radioBusqueda;
            rnd.y = transform.position.y;
            
            float infEnemiga = mapa.GetInfluenciaEnemigaEnMundo(stats.miBando, rnd);
            float infPropia = mapa.GetInfluenciaPropiaEnMundo(stats.miBando, rnd);

            if (buscarBaja)
            {
                // Arqueros: Buscan baja influencia enemiga pero alta propia (zonas seguras rodeadas de aliados)
                float score = infEnemiga - infPropia;
                if (score < mejorScore) { mejorScore = score; mejorPos = rnd; }
                if (infEnemiga > 0.1f) hayCombateCerca = true; // Solo si hay presencia enemiga
            }
            else
            {
                // Resto (Vanguardia): Buscan la mayor concentración enemiga.
                // Restamos un poco la influencia propia para que NO se queden cómodos atrás
                // en la retaguardia con sus aliados. Esto les obliga a empujar la primera línea.
                float score = infEnemiga - (infPropia * 0.2f); 
                if (score > mejorScore) { mejorScore = score; mejorPos = rnd; }
                if (infEnemiga > 0.1f) hayCombateCerca = true; // Solo si hay presencia enemiga
            }
        }

        // EL CAMBIO CLAVE:
        // Si han nacido en base y no hay influencia de combate en su radio de 25m, 
        // en lugar de moverse aleatoriamente, avanzan rectos hacia las torres enemigas
        // hasta que encuentren a "los otros".
        if (!hayCombateCerca && waypoints != null)
        {
            return waypoints.GetObjetivoMasCercano(bandoEnemigo, transform.position);
        }

        return mejorPos;
    }

    private void OnDrawGizmos()
    {
        if (!EstadoTacticoGlobal.DebugActivo || stats == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.radioPercepcion);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.rangoAtaque);
    }
}
