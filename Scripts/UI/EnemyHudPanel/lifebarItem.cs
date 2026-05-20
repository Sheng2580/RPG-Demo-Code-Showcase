using UnityEngine;
using UnityEngine.UI;

public class lifebarItem : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    private EnemyBase enemy;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
    }

    public void SetTarget(EnemyBase target)
    {
        enemy = target;
        RefreshHp();
    }

    public void ClearTarget()
    {
        enemy = null;
        gameObject.SetActive(false);
    }

    public void RefreshHp()
    {
        if (slider == null || enemy == null)
        {
            return;
        }

        slider.value = enemy.maxHp > 0f ? enemy.currentHp / enemy.maxHp : 0f;
    }

    public bool TickFollow(RectTransform panelRoot, Camera camera)
    {
        if (enemy == null || enemy.isDead || panelRoot == null || camera == null || rectTransform == null)
        {
            gameObject.SetActive(false);
            return false;
        }

        Vector3 screenPos = camera.WorldToScreenPoint(enemy.transform.position + worldOffset);
        bool visible =
            screenPos.z > 0f &&
            screenPos.x >= 0f && screenPos.x <= Screen.width &&
            screenPos.y >= 0f && screenPos.y <= Screen.height;

        gameObject.SetActive(visible);
        if (!visible)
        {
            return true;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRoot, screenPos, null, out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint;
        RefreshHp();
        return true;
    }
}


