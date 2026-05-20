using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class specialTipPanel : BasePanel
{
   private Text _tipText;
   private GameObject _eyeImage;
   private Tweener _eyeTweener;
   private Tweener _moveTweener;
   public override void Awake()
   {
      base.Awake();
      _tipText = transform.GetChild(1).GetComponent<Text>();
      _eyeImage = transform.GetChild(0).transform.GetChild(0).gameObject;

   }

   private void OnEnable()
   {
      _eyeImage.transform.localScale = new Vector3(_eyeImage.transform.localScale.x, 0.1f, _eyeImage.transform.localScale.z);
      _moveTweener = transform.DOLocalMove(new Vector3(-752, 447, 0), 0.5f)
         .SetAutoKill(false)
         .OnRewind(OnMoveBackFinished)
         .OnComplete(StartEyeTween);
      _moveTweener.PlayForward();
   }

   public override void Hide()
   {
      _moveTweener.PlayBackwards();
   }

   private void OnDisable()
   {
      _eyeTweener?.Kill();
      _moveTweener?.Kill();
      _eyeImage.transform.localScale = new Vector3(_eyeImage.transform.localScale.x, 1f, _eyeImage.transform.localScale.z);
   }

   private void OnMoveBackFinished()
   {
      base.Hide();
   }

   private void StartEyeTween()
   {
      _eyeTweener?.Kill();
      _eyeImage.transform.localScale = new Vector3(_eyeImage.transform.localScale.x, 0.1f, _eyeImage.transform.localScale.z);
      _eyeTweener = _eyeImage.transform.DOScaleY(1f, 0.5f)
         .SetLoops(3, LoopType.Yoyo)
         .SetEase(Ease.InOutSine)
         .OnComplete(() => {
            _eyeImage.transform.localScale = new Vector3(_eyeImage.transform.localScale.x, 1f, _eyeImage.transform.localScale.z);
         });
   }

   public void SetTipText(string tip)
   {
      _tipText.text = tip;
   }

   public static specialTipPanel Open(string tip, UILayer layer = UILayer.Dynamic)
   {
      if (UIManager.Instance == null) return null;
      var panel = UIManager.Instance.GetPanel<specialTipPanel>();
      if (panel != null)
      {
         panel.SetTipText(tip);
      }
      UIManager.Instance.OpenPanelAsync<specialTipPanel>(layer, loadedPanel =>
      {
         if (loadedPanel != null)
         {
            loadedPanel.SetTipText(tip);
         }
      });
      return panel;
   }
}


