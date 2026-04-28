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
    // ----------------------------------------------------------------------
    private static readonly float[,] FAD = new float[5, 5]
    {
        // Defensor:  Cab    Arq    Lan    Tan    Exp
        /* Cab */   { 1.00f, 1.50f, 0.75f, 0.50f, 1.25f },
        /* Arq */   { 1.00f, 1.00f, 1.25f, 1.00f, 0.75f },
        /* Lan */   { 1.50f, 0.50f, 1.00f, 1.50f, 1.00f },
        /* Tan */   { 1.50f, 1.25f, 0.50f, 1.00f, 1.75f },
        /* Exp */   { 0.75f, 1.75f, 1.00f, 0.25f, 1.00f }
    };

    // ----------------------------------------------------------------------
    // FTA: Factor por terreno del Atacante.
    // Indices: Bioma => Pradera=0, Camino=1, Bosque=2, Urbano=3
    // FTA[bioma, tipoUnidad]
    // ----------------------------------------------------------------------
    private static readonly float[,] FTA = new float[4, 5]
    {
        //              Cab    Arq    Lan    Tan    Exp
        /* Pradera */ { 1.00f, 0.75f, 1.00f, 1.00f, 1.00f },
        /* Camino  */ { 1.25f, 1.00f, 1.00f, 1.50f, 1.00f },
        /* Bosque  */ { 0.50f, 0.50f, 0.75f, 0.25f, 1.50f },
        /* Urbano  */ { 0.75f, 1.50f, 0.75f, 0.50f, 1.25f }
    };

    // ----------------------------------------------------------------------
    // FTD: Factor por terreno del Defensor.
    // FTD[bioma, tipoUnidad]
    // ----------------------------------------------------------------------
    private static readonly float[,] FTD = new float[4, 5]
    {
        //              Cab    Arq    Lan    Tan    Exp
        /* Pradera */ { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* Camino  */ { 0.75f, 0.75f, 0.75f, 0.50f, 1.25f },
        /* Bosque  */ { 1.25f, 1.50f, 1.50f, 1.75f, 1.50f },
        /* Urbano  */ { 1.50f, 0.75f, 1.50f, 1.75f, 0.75f }
    };

    public static float ObtenerFAD(TipoUnidad atacante, TipoUnidad defensor)
        => FAD[(int)atacante, (int)defensor];

    public static float ObtenerFTA(Bioma bioma, TipoUnidad tipo)
        => FTA[(int)bioma, (int)tipo];

    public static float ObtenerFTD(Bioma bioma, TipoUnidad tipo)
        => FTD[(int)bioma, (int)tipo];

    // FA = Calidad * FAD * FTA
    public static float CalcularFA(NPCStats atacante, Bioma terrenoAtacante, NPCStats defensor)
    {
        return atacante.poder
             * ObtenerFAD(atacante.tipoUnidad, defensor.tipoUnidad)
             * ObtenerFTA(terrenoAtacante, atacante.tipoUnidad);
    }

    // FD = Calidad * FTD
    public static float CalcularFD(NPCStats defensor, Bioma terrenoDefensor)
    {
        return defensor.poder * ObtenerFTD(terrenoDefensor, defensor.tipoUnidad);
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
