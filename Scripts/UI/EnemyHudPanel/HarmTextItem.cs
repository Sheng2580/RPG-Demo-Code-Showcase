using TMPro;
using UnityEngine;

public class HarmTextItem : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.55f, 0f);
    [SerializeField] private Vector2 randomScreenOffset = new Vector2(-26f, 26f);
    [SerializeField] private float lifeTime = 0.55f;
    [SerializeField] private float floatDistance = 42f;
    [SerializeField] private Color normalColor = new Color(0.9f, 0.95f, 1f, 1f);
    [SerializeField] private Color critColor = new Color(1f, 0.72f, 0.16f, 1f);
    [SerializeField] private float critScale = 1.25f;

    private RectTransform rectTransform;
    private RectTransform panelRoot;
    private Camera followCamera;
    private Transform target;
    private Vector2 startAnchoredPosition;
    private float timer;
    private bool isCrit;
    private float baseScale = 1f;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (damageText == null)
        {
            damageText = GetComponent<TMP_Text>();
        }
    }

    public void Play(EnemyBase enemy, float damage, bool crit, RectTransform root, Camera camera)
    {
        target = enemy != null ? enemy.transform : null;
        panelRoot = root;
        followCamera = camera;
        isCrit = crit;
        timer = 0f;
        baseScale = isCrit ? critScale : 1f;

        if (damageText != null)
        {
            damageText.text = isCrit
                ? $"{Mathf.CeilToInt(damage)}!"
                : Mathf.CeilToInt(damage).ToString();

            Color color = isCrit ? critColor : normalColor;
            color.a = 1f;
            damageText.color = color;
        }

        transform.localScale = Vector3.one * baseScale;
        gameObject.SetActive(true);
        UpdateStartPosition();
    }

    public bool Tick()
    {
        timer += Time.deltaTime;
        float progress = lifeTime > 0f ? Mathf.Clamp01(timer / lifeTime) : 1f;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startAnchoredPosition + Vector2.up * (floatDistance * progress);
        }

        if (damageText != null)
        {
            Color color = damageText.color;
            color.a = 1f - progress;
            damageText.color = color;
        }

        float popScale = Mathf.Lerp(1f, 1.18f, 1f - Mathf.Abs(progress - 0.25f) * 4f);
        transform.localScale = Vector3.one * baseScale * popScale;

        return progress >= 1f;
    }

    public void Clear()
    {
        target = null;
        panelRoot = null;
        followCamera = null;
        isCrit = false;
        baseScale = 1f;
        gameObject.SetActive(false);
    }

    private void UpdateStartPosition()
    {
        if (target == null || panelRoot == null || followCamera == null || rectTransform == null)
        {
            startAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            return;
        }

        Vector3 screenPos = followCamera.WorldToScreenPoint(target.position + worldOffset);
        if (screenPos.z <= 0f)
        {
            startAnchoredPosition = rectTransform.anchoredPosition;
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRoot, screenPos, null, out Vector2 localPoint))
        {
            localPoint += new Vector2(Random.Range(randomScreenOffset.x, randomScreenOffset.y), 0f);
            startAnchoredPosition = localPoint;
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }
}


