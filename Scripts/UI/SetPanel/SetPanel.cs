using UnityEngine;

public class SetPanel : BasePanel
{
   private RectTransform _center;
   private PlayerProperty _playerProperty;
   private BuffKnapsack _buffKnapsack;
   private Prop _prop;
   private Set _set;

   public override void Awake()
   {
      base.Awake();
      BindPages();
   }

   private void OnEnable()
   {
      BindPages();
      PlayerNumericManager.Instance.EnsureRuntimeInitialized();
      PlayerNumericManager.Instance.ApplyToCurrentPlayer();
      ShowDefaultPage();
      RefreshNumericViews();
   }

   public override void Show()
   {
      base.Show();
      specialTipPanel.Open("璁剧疆");
   }

   public override void Hide()
   {
      UIManager.Instance.ClosePanel<specialTipPanel>();
      base.Hide();
   }

   private void Start()
   {
      BindPages();
   }

   private void BindPages()
   {
      Transform centerTrans = transform.Find("Center");
      if (centerTrans == null)
      {
         Debug.LogWarning("[SetPanel] Center not found.");
         return;
      }

      _center = centerTrans.GetComponent<RectTransform>();
      _playerProperty = GetPageComponent<PlayerProperty>("PlayerProperty");
      _buffKnapsack = GetPageComponent<BuffKnapsack>("BuffKnapsack");
      _prop = GetPageComponent<Prop>("Prop");
      _set = GetPageComponent<Set>("Set");
   }

   private T GetPageComponent<T>(string pageName) where T : Component
   {
      Transform page = _center != null ? _center.Find(pageName) : null;
      return page != null ? page.GetComponent<T>() : null;
   }

   public void RefreshNumericViews()
   {
      _playerProperty?.Refresh();
      _buffKnapsack?.Refresh();
      _prop?.Refresh();
   }

   private void ShowDefaultPage()
   {
      SetPageActive(_playerProperty, true);
      SetPageActive(_buffKnapsack, false);
      SetPageActive(_prop, false);
      SetPageActive(_set, false);
   }

   private void SetPageActive(Component page, bool active)
   {
      if (page != null && page.gameObject.activeSelf != active)
      {
         page.gameObject.SetActive(active);
      }
   }
}


