using UnityEngine;

// Sistema de combate basado en el Anexo II de la practica.
// Da�o = (FA / FD) * CteImpacto * aleatorio,  con probabilidad de cr�tico.
//   FA = Calidad(atacante) * FAD * FTA
//   FD = Calidad(defensor) * FTD
public static class SistemaCombate
{
    // Constante de impacto: ajustar seg�n vidaMax. Con vidaMax ~1000 y CTE_IMPACTO=100
    // un combate equilibrado dura ~10-15 ataques.
    public const float CTE_IMPACTO = 100f;

    // Probabilidad de cr�tico: 1 entre PROB_CRITICO. M�ximo da�o = CTE_IMPACTO * MULT_CRITICO.
    public const int PROB_CRITICO = 100;
    public const float MULT_CRITICO = 50f;

    // ----------------------------------------------------------------------
    // FAD: Factor por tipo de Atacante / Defensor.
    // Indices: TipoUnidad => Caballero=0, Arquero=1, Lancero=2, Tanque=3, Explorador=4
    // FAD[atacante, defensor]
    //
    // Valores acordados con la memoria (orden alfabetico en la memoria, aqui
    // permutado al orden del enum). La memoria muestra:
    //                Arq    Cab    Exp    Lan    Tan
    //   Arq atk      1.00   0.75   1.25   1.50   0.50
    //   Cab atk      1.50   1.00   0.75   0.50   1.25
    //   Exp atk      1.25   0.50   1.00   0.75   1.50
    //   Lan atk      0.50   1.50   1.25   1.00   0.75
    //   Tan atk      1.50   1.25   0.50   0.75   1.00
    // ----------------------------------------------------------------------
    private static readonly float[,] FAD = new float[5, 5]
    {
        // Defensor:  Cab    Arq    Lan    Tan    Exp
        /* Cab */   { 1.00f, 1.50f, 0.50f, 1.25f, 0.75f },
        /* Arq */   { 0.75f, 1.00f, 1.50f, 0.50f, 1.25f },
        /* Lan */   { 1.50f, 0.50f, 1.00f, 0.75f, 1.25f },
        /* Tan */   { 1.25f, 1.50f, 0.75f, 1.00f, 0.50f },
        /* Exp */   { 0.50f, 1.25f, 0.75f, 1.50f, 1.00f }
    };

    // ----------------------------------------------------------------------
    // FTA: Factor por terreno del Atacante.
    // Indices: Bioma => Pradera=0, Camino=1, Bosque=2, Urbano=3
    // FTA[bioma, tipoUnidad]
    //
    // Valores de la memoria para Pradera/Bosque/Urbano. Camino se anade
    // siguiendo las afinidades narradas en la introduccion (Caballero
    // prefiere camino, Tanque y Explorador lo evitan).
    // ----------------------------------------------------------------------
    private static readonly float[,] FTA = new float[4, 5]
    {
        //              Cab    Arq    Lan    Tan    Exp
        /* Pradera */ { 1.50f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* Camino  */ { 1.25f, 1.00f, 1.00f, 0.75f, 0.75f },
        /* Bosque  */ { 0.75f, 1.50f, 0.75f, 0.50f, 1.25f },
        /* Urbano  */ { 0.50f, 0.75f, 1.25f, 1.50f, 1.25f }
    };

    // ----------------------------------------------------------------------
    // FTD: Factor por terreno del Defensor.
    // FTD[bioma, tipoUnidad]
    // ----------------------------------------------------------------------
    private static readonly float[,] FTD = new float[4, 5]
    {
        //              Cab    Arq    Lan    Tan    Exp
        /* Pradera */ { 0.75f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* Camino  */ { 1.25f, 1.00f, 1.00f, 0.75f, 0.75f },
        /* Bosque  */ { 1.25f, 0.50f, 1.25f, 1.50f, 0.75f },
        /* Urbano  */ { 1.50f, 1.25f, 0.75f, 0.50f, 0.75f }
    };

    public static float ObtenerFAD(TipoUnidad atacante, TipoUnidad defensor)
        => FAD[(int)atacante, (int)defensor];

    public static float ObtenerFTA(Bioma bioma, TipoUnidad tipo)
        => FTA[(int)bioma, (int)tipo];

    public static float ObtenerFTD(Bioma bioma, TipoUnidad tipo)
        => FTD[(int)bioma, (int)tipo];

    public static float CalcularFA(NPCStats atacante, Bioma terrenoAtacante, NPCStats defensor)
    {
        float poderAtacante = atacante.poder;
        estadoNPC est = atacante.GetComponent<estadoNPC>();
        if (est != null && est.GetEstadoActual() == EstadoNPC.Defensa && atacante.tipoUnidad == TipoUnidad.Tanque)
        {
            PercepcionNPC perc = atacante.GetComponent<PercepcionNPC>();
            if (perc != null && !perc.TieneAliadosCerca()) poderAtacante *= 1.25f;
        }

        if (est != null && est.GetEstadoActual() == EstadoNPC.Ataque)
        {
            if (atacante.tipoUnidad == TipoUnidad.Caballero)
            {
                if (atacante.VidaActual < (atacante.VidaMax * 0.5f)) poderAtacante *= 1.20f;
            }
            else if (atacante.tipoUnidad == TipoUnidad.Lancero)
            {
                PercepcionNPC perc = atacante.GetComponent<PercepcionNPC>();
                if (perc != null && perc.TieneAliadosCerca()) poderAtacante *= 1.20f;
            }
        }

        return poderAtacante
             * ObtenerFAD(atacante.tipoUnidad, defensor.tipoUnidad)
             * ObtenerFTA(terrenoAtacante, atacante.tipoUnidad);
    }

    public static float CalcularFD(NPCStats defensor, Bioma terrenoDefensor)
    {
        float poderDefensor = defensor.poder;
        estadoNPC est = defensor.GetComponent<estadoNPC>();
        
        if (est != null && est.GetEstadoActual() == EstadoNPC.Defensa)
        {
            if (defensor.tipoUnidad == TipoUnidad.Tanque)
            {
                PercepcionNPC perc = defensor.GetComponent<PercepcionNPC>();
                if (perc != null && !perc.TieneAliadosCerca()) poderDefensor *= 1.25f;
            }
        }

        float fd = poderDefensor * ObtenerFTD(terrenoDefensor, defensor.tipoUnidad);

        if (est != null && est.GetEstadoActual() == EstadoNPC.Defensa && defensor.tipoUnidad == TipoUnidad.Lancero)
        {
            AgentNPC agent = defensor.GetComponent<AgentNPC>();
            if (agent != null && agent.Velocity.sqrMagnitude < 0.1f)
            {
                fd *= 1.25f; // Aumentar defensa 25% equivale a reducir daño un 20%
            }
        }

        return fd;
    }

    public struct ResultadoAtaque
    {
        public float danio;
        public bool esCritico;
        public float fa;
        public float fd;
    }

    // F�rmula del Anexo II:
    //   Si Random(100)==100  => Da�o = CteImpacto * 50    (cr�tico)
    //   Si no                => Da�o = (FA/FD) * CteImpacto * (Random(50)/100 + 0.5)
    //   Si Da�o <= CteImpacto/10 => Da�o = Random(CteImpacto/10) + CteImpacto/10
    public static ResultadoAtaque CalcularDanio(NPCStats atacante, Bioma terrenoAtacante,
                                                NPCStats defensor, Bioma terrenoDefensor)
    {
        ResultadoAtaque res = new ResultadoAtaque
        {
            fa = CalcularFA(atacante, terrenoAtacante, defensor),
            fd = CalcularFD(defensor, terrenoDefensor)
        };

        // Cr�tico: 1 entre PROB_CRITICO
        if (Random.Range(0, PROB_CRITICO) == 0)
        {
            res.danio = CTE_IMPACTO * MULT_CRITICO;
            res.esCritico = true;
            return res;
        }

        if (res.fd <= 0f) res.fd = 1f;

        float aleatorio = Random.Range(0, 50) / 100f + 0.5f; // [0.5, 1.0]
        res.danio = (res.fa / res.fd) * CTE_IMPACTO * aleatorio;

        // M�nimo de da�o: entre 10% y 20% de la cte de impacto
        float danioMin = CTE_IMPACTO / 10f;
        if (res.danio <= danioMin)
        {
            res.danio = Random.Range(0f, danioMin) + danioMin;
        }

        return res;
    }
}
