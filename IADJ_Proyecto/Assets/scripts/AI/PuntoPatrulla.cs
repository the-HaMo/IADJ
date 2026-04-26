using UnityEngine;

// Pon este script en cada objeto vacio que uses como punto de patrulla.
// Dibuja una esfera negra visible en el juego para poder verlos en el mapa.
public class PuntoPatrulla : MonoBehaviour
{
    public float radio = 0.4f;

    private void Start()
    {
        // Creamos una esfera negra en la posicion del objeto
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.transform.SetParent(transform);
        esfera.transform.localPosition = Vector3.zero;
        esfera.transform.localScale = Vector3.one * radio;

        // Quitamos el collider para que no interfiera con nada
        Destroy(esfera.GetComponent<Collider>());

        // Ponemos el color negro
        Renderer r = esfera.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.black;
        r.material = mat;
    }
}
