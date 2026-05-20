using System;
using UnityEngine;

public class merchantController : NPCBase, IStateMachineOwner
{
   private NPCModle _npcModle;

   protected override void Start()
   {
      base.Start();
      ChangeState(NPCStateType.idle);
      InitActions();
      _npcModle = transform.GetChild(0).gameObject.GetComponent<NPCModle>();
   }

   protected override void Update()
   {
      base.Update();
   }

   public void InitActions()
   {
      if (GameSceneManager.Instance.GetCurrSceneName() == "hall")
      {
         AddAction("瀵硅瘽", OpenDialoguePanel);
         AddAction("鎵撳紑鍟嗗簵", () => TestFun(1));
      }
      else
      {
         AddAction("鎵撳紑鍟嗗簵", () =>
         {
            EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? false);
            EventCenter.Instance.EventTrigger(GameEvent.瑙掕壊鎴樻枟鎺у埗,false);
            UIManager.Instance.OpenPanel<merchantPanel>();
         });
      }
   }

   private void TestFun(int n)
   {
      Debug.Log("TestFun " + n);
      EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁鎽勫儚鏈? false);
      EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? false);
      _npcModle.lookAtCamera.gameObject.SetActive(true);
      HallManager.Instance.currentCamera = _npcModle.lookAtCamera;
      bool prevHeadRota = _npcModle != null && _npcModle.isHeadRota;
      if (_npcModle != null)
      {
         _npcModle.SetHeadRotationEnabled(false);
         _npcModle.FaceForwardImmediate();
      }

      PostProcessingManager.Instance?.AnimateDepthOfFieldTo(0.83f, 0.6f);

      UIManager.Instance.OpenPanelAsync<merchantPanel>(UILayer.Top, panel =>
      {
         if (panel == null)
         {
            Debug.LogWarning("[merchantController] Open merchantPanel failed.");
            return;
         }

         Action onClose = null;
         onClose = () =>
         {
            if (_npcModle != null)
            {
               _npcModle.SetHeadRotationEnabled(prevHeadRota);
            }

            EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁鎽勫儚鏈? true);
            EventCenter.Instance.EventTrigger(GameEvent.璁剧疆鐜╁杈撳叆鐘舵€? true);
            _npcModle.lookAtCamera.gameObject.SetActive(false);
            EventCenter.Instance.EventTrigger(GameEvent.鐜╁妫€娴婲pc);
            PostProcessingManager.Instance?.AnimateDepthOfFieldTo(10f, 0.6f);
            panel.OnPanelClosed -= onClose;
         };

         panel.OnPanelClosed += onClose;
      });
   }
}


