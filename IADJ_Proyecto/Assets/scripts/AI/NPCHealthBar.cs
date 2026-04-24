using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NPCStats))]
public class NPCHealthBar : MonoBehaviour
{
    [Header("Posicion")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 3f, 0f);

    [Header("Tamano")]
    [SerializeField] private float width = 80f;
    [SerializeField] private float height = 12f;

    [Header("Colores")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color lostHealthColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    private NPCStats stats;
    private Transform uiRoot;
    private RectTransform currentFillRect;
    private RectTransform lostFillRect;
    private Camera mainCam;

    private void Awake()
    {
        stats = GetComponent<NPCStats>();
        mainCam = Camera.main;
        CreateHealthBar();

        if (stats != null)
        {
            ActualizarTamanoBarra(stats.VidaPorcentaje);
        }
    }

    private void OnEnable()
    {
        if (stats == null)
        {
            stats = GetComponent<NPCStats>();
        }

        if (stats != null)
        {
            stats.OnVidaCambiada += OnVidaCambiada;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnVidaCambiada -= OnVidaCambiada;
        }
    }

    private void LateUpdate()
    {
        if (uiRoot == null || currentFillRect == null || lostFillRect == null || stats == null)
        {
            return;
        }

        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        uiRoot.position = transform.position + worldOffset;

        if (mainCam != null)
        {
            Vector3 lookDir = uiRoot.position - mainCam.transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                uiRoot.rotation = Quaternion.LookRotation(lookDir);
            }
        }

    }

    private void OnVidaCambiada(float vidaActual, float vidaMax, bool fueDanio)
    {
        float porcentaje = vidaMax > 0f ? vidaActual / vidaMax : 0f;
        ActualizarTamanoBarra(porcentaje);
    }

    private void ActualizarTamanoBarra(float porcentaje)
    {
        if (currentFillRect == null || lostFillRect == null)
        {
            return;
        }

        float percent = Mathf.Clamp01(porcentaje);
        lostFillRect.sizeDelta = new Vector2(width, height);
        currentFillRect.sizeDelta = new Vector2(width * percent, height);
    }

    private void CreateHealthBar()
    {
        GameObject rootObj = new GameObject("HealthBar");
        rootObj.transform.SetParent(transform, false);
        uiRoot = rootObj.transform;
        uiRoot.position = transform.position + worldOffset;

        Canvas canvas = rootObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        rootObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(width, height);
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(rootObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject lostObj = new GameObject("LostHealth");
        lostObj.transform.SetParent(bgObj.transform, false);
        Image lostImage = lostObj.AddComponent<Image>();
        lostImage.color = lostHealthColor;

        lostFillRect = lostObj.GetComponent<RectTransform>();
        lostFillRect.anchorMin = new Vector2(0f, 0f);
        lostFillRect.anchorMax = new Vector2(0f, 1f);
        lostFillRect.pivot = new Vector2(0f, 0.5f);
        lostFillRect.anchoredPosition = Vector2.zero;
        lostFillRect.sizeDelta = new Vector2(width, height);

        GameObject currentObj = new GameObject("CurrentHealth");
        currentObj.transform.SetParent(bgObj.transform, false);
        Image currentImage = currentObj.AddComponent<Image>();
        currentImage.color = fillColor;

        currentFillRect = currentObj.GetComponent<RectTransform>();
        currentFillRect.anchorMin = new Vector2(0f, 0f);
        currentFillRect.anchorMax = new Vector2(0f, 1f);
        currentFillRect.pivot = new Vector2(0f, 0.5f);
        currentFillRect.anchoredPosition = Vector2.zero;
        currentFillRect.sizeDelta = new Vector2(width, height);
    }
}
