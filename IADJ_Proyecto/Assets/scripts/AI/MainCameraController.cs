using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MainCameraController : MonoBehaviour
{
    [Header("Transform Inicial")]
    public bool applyInitialTransformOnPlay = true;
    public Vector3 initialPosition = new Vector3(16f, 115f, 245f);
    public Vector3 initialEulerAngles = new Vector3(149.567f, 9.315f, 180f);

    [Header("Movimiento")]
    public float moveSpeed = 40f;
    public float fastMoveMultiplier = 10f;

    [Header("Zoom")]
    public float scrollZoomSpeed = 50f;
    public float minHeight = 12f;
    public float maxHeight = 120f;

    [Header("Limites del Mapa")]
    public bool clampToMap = true;
    public Vector2 mapMinXZ = new Vector2(-160f, -160f);
    public Vector2 mapMaxXZ = new Vector2(160f, 245f);
    public bool forceLimitsFromCodeOnPlay = true;
    public Vector2 forcedMapMinXZ = new Vector2(-160f, -160f);
    public Vector2 forcedMapMaxXZ = new Vector2(160f, 300f);

    private void Start()
    {
        if (forceLimitsFromCodeOnPlay)
        {
            mapMinXZ = forcedMapMinXZ;
            mapMaxXZ = forcedMapMaxXZ;
        }

        if (!applyInitialTransformOnPlay)
        {
            return;
        }

        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(initialEulerAngles);
        ClampPosition();
    }

    private void Update()
    {
        HandleMove();
        HandleScrollZoom();
        ClampPosition();
    }

    private void HandleMove()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;

        // Utilizamos la rotación en el eje Y de la cámara para que el movimiento
        // siempre se corresponda con hacia dónde mira la cámara, independientemente del pitch
        Quaternion yRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 forward = yRotation * Vector3.forward;
        Vector3 right = yRotation * Vector3.right;

        Vector3 moveDir = (right * h + forward * v);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speed *= fastMoveMultiplier;
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        // Avanza/retrocede en la direccion de la camara para mantener sensacion RTS.
        Vector3 next = transform.position + transform.forward * (scroll * scrollZoomSpeed);
        next.y = Mathf.Clamp(next.y, minHeight, maxHeight);

        if (clampToMap)
        {
            next.x = Mathf.Clamp(next.x, mapMinXZ.x, mapMaxXZ.x);
            next.z = Mathf.Clamp(next.z, mapMinXZ.y, mapMaxXZ.y);
        }

        transform.position = next;
    }

    private void ClampPosition()
    {
        Vector3 p = transform.position;
        p.y = Mathf.Clamp(p.y, minHeight, maxHeight);

        if (clampToMap)
        {
            p.x = Mathf.Clamp(p.x, mapMinXZ.x, mapMaxXZ.x);
            p.z = Mathf.Clamp(p.z, mapMinXZ.y, mapMaxXZ.y);
        }

        transform.position = p;
    }
}