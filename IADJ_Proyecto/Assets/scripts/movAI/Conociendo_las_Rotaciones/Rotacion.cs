using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotacion : MonoBehaviour
{
    public float angulo = 0;

    // Update is called once per frame
    void Update()
    {
        //transform.rotation = Quaternion.AngleAxis(valor, Vector3.up);

        //transform.rotation = Quaternion.Euler(0, valor, 0);

        transform.rotation = new Quaternion(); //Quaternion.identity;
        transform.Rotate(Vector3.up, angulo);
    }
}
