using UnityEngine;

// Perfil tactico de cada tipo de unidad. Resuelve el requisito (a) del Bloque 2:
// "cada unidad debe tener un componente tactico de toma de decisiones particularizado
//  al tipo de unidad".
//
// PercepcionNPC consulta este perfil para sesgar sus decisiones segun el tipo:
//   - Cuando huir (vidaCriticaPct).
//   - A que distancia se planta del enemigo (distanciaCombatePref).
//   - Que tipos de enemigo prioriza (objetivosPreferidos).
//   - Si prefiere quedarse en cierto bioma (biomaPreferido / biomaEvitar).
public static class TacticasPorTipo
{
    public struct PerfilTactico
    {
        public float vidaCriticaPct;       // % vida bajo el cual considera huir
        public float distanciaCombatePref; // factor sobre rangoAtaque (1.0 = se planta a quemarropa, 2.0 = mantiene distancia)
        public TipoUnidad[] objetivosPreferidos; // tipos enemigos que ataca antes que otros
        public Bioma biomaPreferido;       // donde rinde mejor
        public Bioma biomaEvitar;          // donde rinde peor
        public float bonusAgresividadEnPreferido; // multiplicador a radioPercepcion en su bioma
        public bool aguantaInfluenciaEnemiga; // los tanques no huyen por influencia
    }

    public static PerfilTactico GetPerfil(TipoUnidad tipo)
    {
        switch (tipo)
        {
            case TipoUnidad.Caballero:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.30f,
                    distanciaCombatePref = 1.0f,
                    objetivosPreferidos = new[] { TipoUnidad.Arquero, TipoUnidad.Explorador },
                    biomaPreferido = Bioma.Pradera,
                    biomaEvitar = Bioma.Bosque,
                    bonusAgresividadEnPreferido = 1.2f,
                    aguantaInfluenciaEnemiga = false
                };

            case TipoUnidad.Arquero:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.45f, // huye antes
                    distanciaCombatePref = 1.6f, // mantiene distancia (kiting)
                    objetivosPreferidos = new[] { TipoUnidad.Lancero, TipoUnidad.Caballero },
                    biomaPreferido = Bioma.Pradera, // alcance abierto
                    biomaEvitar = Bioma.Bosque,    // pierde mucho FTA en bosque
                    bonusAgresividadEnPreferido = 1.4f,
                    aguantaInfluenciaEnemiga = false
                };

            case TipoUnidad.Lancero:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.25f,
                    distanciaCombatePref = 1.0f,
                    objetivosPreferidos = new[] { TipoUnidad.Tanque, TipoUnidad.Caballero },
                    biomaPreferido = Bioma.Urbano,
                    biomaEvitar = Bioma.Pradera,
                    bonusAgresividadEnPreferido = 1.1f,
                    aguantaInfluenciaEnemiga = false
                };

            case TipoUnidad.Tanque:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.15f, // aguanta hasta el final
                    distanciaCombatePref = 1.0f,
                    objetivosPreferidos = new[] { TipoUnidad.Lancero, TipoUnidad.Caballero, TipoUnidad.Arquero },
                    biomaPreferido = Bioma.Camino,
                    biomaEvitar = Bioma.Bosque,
                    bonusAgresividadEnPreferido = 1.3f,
                    aguantaInfluenciaEnemiga = true  // se queda aunque haya enemigos cerca
                };

            case TipoUnidad.Explorador:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.50f, // huye facil
                    distanciaCombatePref = 1.2f,
                    objetivosPreferidos = new[] { TipoUnidad.Arquero }, // ataca solo blandos
                    biomaPreferido = Bioma.Bosque, // x1.5 en bosque atacando
                    biomaEvitar = Bioma.Camino,
                    bonusAgresividadEnPreferido = 1.5f,
                    aguantaInfluenciaEnemiga = false
                };

            default:
                return new PerfilTactico
                {
                    vidaCriticaPct = 0.30f,
                    distanciaCombatePref = 1.0f,
                    objetivosPreferidos = new TipoUnidad[0],
                    biomaPreferido = Bioma.Pradera,
                    biomaEvitar = Bioma.Pradera,
                    bonusAgresividadEnPreferido = 1.0f,
                    aguantaInfluenciaEnemiga = false
                };
        }
    }

    // Devuelve true si "candidato" esta en la lista de objetivos preferidos del perfil.
    public static bool EsObjetivoPreferido(PerfilTactico perfil, TipoUnidad candidato)
    {
        if (perfil.objetivosPreferidos == null) return false;
        foreach (var t in perfil.objetivosPreferidos)
        {
            if (t == candidato) return true;
        }
        return false;
    }
}
