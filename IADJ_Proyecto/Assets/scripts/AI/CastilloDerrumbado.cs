using UnityEngine;

public class CastilloDerrumbado : MonoBehaviour
{
    public Transform CastilloRojo;
    public Transform CastilloAzul;
    public GameObject AzulVictoria;
    public GameObject RojoVictoria;
    
    private bool juegoTerminado = false;

    void Update()
    {
        // Si ya ha ganado alguien, dejamos de comprobar
        if (juegoTerminado) return;

        if (CastilloAzul == null)
        {
            ActivarConPadre(RojoVictoria);
            if (AzulVictoria != null) AzulVictoria.SetActive(false);
            juegoTerminado = true;
            TerminarJuego();
        }
        else if (CastilloRojo == null)
        {
            ActivarConPadre(AzulVictoria);
            if (RojoVictoria != null) RojoVictoria.SetActive(false);
            juegoTerminado = true;
            TerminarJuego();
        }
    }

    private void ActivarConPadre(GameObject objeto)
    {
        if (objeto == null) return;

        if (objeto.transform.parent != null)
        {
            objeto.transform.parent.gameObject.SetActive(true);
        }

        objeto.SetActive(true);
    }

    private void TerminarJuego()
    {
        Time.timeScale = 0f;
    }
}
