using UnityEngine;

public class CastilloDerrumbado : MonoBehaviour
{
    public Transform CastilloRojo;
    public Transform CastilloAzul;
    
    private bool juegoTerminado = false;

    void Update()
    {
        // Si ya ha ganado alguien, dejamos de comprobar
        if (juegoTerminado) return;

        if (CastilloAzul == null)
        {
            Debug.Log("<color=red>¡El equipo ROJO ha ganado el juego!</color>");
            juegoTerminado = true;
            TerminarJuego();
        }
        else if (CastilloRojo == null)
        {
            Debug.Log("<color=blue>¡El equipo AZUL ha ganado el juego!</color>");
            juegoTerminado = true;
            TerminarJuego();
        }
    }

    private void TerminarJuego()
    {
        Time.timeScale = 0f;
    }
}
