using System.Collections.Generic;
using UnityEngine;

public class EnemyHudPanel : BasePanel
{
    private const string ItemAbName = "uiitem";
    private const string HealthBarItemName = "lifebarItem";
    private const string DamageTextItemName = "HarmTextItem";

    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private RectTransform damageNumberRoot;

    private readonly Dictionary<EnemyBase, lifebarItem> healthBars = new Dictionary<EnemyBase, lifebarItem>();
    private readonly Queue<lifebarItem> healthBarPool = new Queue<lifebarItem>();
    private readonly List<HarmTextItem> activeDamageTexts = new List<HarmTextItem>();
    private readonly Queue<HarmTextItem> damageTextPool = new Queue<HarmTextItem>();
    private RectTransform panelRoot;
    private Camera mainCamera;

    public override void Awake()
    {
        base.Awake();
        panelRoot = transform as RectTransform;

        if (healthBarRoot == null)
        {
            Transform root = transform.Find("HealthBarRoot");
            healthBarRoot = root != null ? root as RectTransform : panelRoot;
        }

        if (damageNumberRoot == null)
        {
            Transform root = transform.Find("DamageNumberRoot");
            damageNumberRoot = root != null ? root as RectTransform : panelRoot;
        }

        StretchRoot(healthBarRoot);
        StretchRoot(damageNumberRoot);
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        foreach (var kvp in healthBars)
        {
            kvp.Value?.TickFollow(panelRoot, mainCamera);
        }

        TickDamageTexts();
    }

    public override void Show()
    {
        base.Show();
        RegisterSceneEnemies();
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null || enemy.isDead || healthBars.ContainsKey(enemy))
        {
            return;
        }

        lifebarItem item = GetHealthBarItem();
        if (item == null)
        {
            return;
        }

        healthBars.Add(enemy, item);
        item.gameObject.SetActive(true);
        item.SetTarget(enemy);
        enemy.OnHpChanged += OnEnemyHpChanged;
        enemy.OnDamaged += OnEnemyDamaged;
        enemy.OnDead += OnEnemyDead;
    }

    private void OnEnemyHpChanged(EnemyBase enemy, float currentHp, float maxHp)
    {
        if (enemy != null && healthBars.TryGetValue(enemy, out lifebarItem item))
        {
            item.RefreshHp();
        }
    }

    private void OnEnemyDamaged(EnemyBase enemy, float damage, bool isCrit)
    {
        if (enemy == null || damage <= 0f)
        {
            return;
        }

        ShowDamageText(enemy, damage, isCrit);
    }

    private void OnEnemyDead(EnemyBase enemy)
    {
        UnregisterEnemy(enemy);
    }

    private void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy == null || !healthBars.TryGetValue(enemy, out lifebarItem item))
        {
            return;
        }

        enemy.OnHpChanged -= OnEnemyHpChanged;
        enemy.OnDamaged -= OnEnemyDamaged;
        enemy.OnDead -= OnEnemyDead;
        healthBars.Remove(enemy);
        item.ClearTarget();
        healthBarPool.Enqueue(item);
    }

    private lifebarItem GetHealthBarItem()
    {
        if (healthBarPool.Count > 0)
        {
            return healthBarPool.Dequeue();
        }

        if (ABManager.Instance == null)
        {
            return null;
        }

        GameObject itemObj = ABManager.Instance.LoadRes<GameObject>(ItemAbName, HealthBarItemName);
        if (itemObj == null)
        {
            return null;
        }

        itemObj.transform.SetParent(healthBarRoot != null ? healthBarRoot : transform, false);
        lifebarItem item = itemObj.GetComponent<lifebarItem>();
        if (item == null)
        {
            Destroy(itemObj);
        }

        return item;
    }

    private void ShowDamageText(EnemyBase enemy, float damage, bool isCrit)
    {
        HarmTextItem item = GetDamageTextItem();
        if (item == null)
        {
            return;
        }

        item.Play(enemy, damage, isCrit, panelRoot, mainCamera != null ? mainCamera : Camera.main);
        activeDamageTexts.Add(item);
    }

    private HarmTextItem GetDamageTextItem()
    {
        if (damageTextPool.Count > 0)
        {
            return damageTextPool.Dequeue();
        }

        if (ABManager.Instance == null)
        {
            return null;
        }

        GameObject itemObj = ABManager.Instance.LoadRes<GameObject>(ItemAbName, DamageTextItemName);
        if (itemObj == null)
        {
            return null;
        }

        itemObj.transform.SetParent(damageNumberRoot != null ? damageNumberRoot : transform, false);
        HarmTextItem item = itemObj.GetComponent<HarmTextItem>();
        if (item == null)
        {
            Destroy(itemObj);
        }

        return item;
    }

    private void TickDamageTexts()
    {
        for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
        {
            HarmTextItem item = activeDamageTexts[i];
            if (item == null || item.Tick())
            {
                activeDamageTexts.RemoveAt(i);
                if (item != null)
                {
                    item.Clear();
                    damageTextPool.Enqueue(item);
                }
            }
        }
    }

    public override void Hide()
    {
        ClearAllEnemies();
        base.Hide();
    }

    private void OnDestroy()
    {
        ClearAllEnemies();
    }

    private void ClearAllEnemies()
    {
        foreach (var kvp in healthBars)
        {
            if (kvp.Key != null)
            {
                kvp.Key.OnHpChanged -= OnEnemyHpChanged;
                kvp.Key.OnDamaged -= OnEnemyDamaged;
                kvp.Key.OnDead -= OnEnemyDead;
            }

            if (kvp.Value != null)
            {
                kvp.Value.ClearTarget();
                healthBarPool.Enqueue(kvp.Value);
            }
        }

        healthBars.Clear();

        for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
        {
            HarmTextItem item = activeDamageTexts[i];
            if (item == null)
            {
                continue;
            }

            item.Clear();
            damageTextPool.Enqueue(item);
        }

        activeDamageTexts.Clear();
    }

    private void RegisterSceneEnemies()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        for (int i = 0; i < enemies.Length; i++)
        {
            RegisterEnemy(enemies[i]);
        }
    }

    private void StretchRoot(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.anchoredPosition = Vector2.zero;
    }
}


