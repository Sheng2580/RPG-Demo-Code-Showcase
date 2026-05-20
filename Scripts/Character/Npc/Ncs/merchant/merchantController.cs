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
      if (SceneMgr.Instance.GetCurrSceneName() == "hall")
      {
         AddAction("对话", OpenDialoguePanel);
         AddAction("打开商店", () => TestFun(1));
      }
      else
      {
         AddAction("打开商店", () =>
         {
            EventCenter.Instance.EventTrigger(GameEvent.设置玩家输入状态, false);
            EventCenter.Instance.EventTrigger(GameEvent.角色战斗控制,false);
            UIManager.Instance.OpenPanel<merchantPanel>();
         });
      }
   }

   //打开对应和退出对应面板的方法
   private void TestFun(int n)
   {
      Debug.Log("TestFun " + n);
      EventCenter.Instance.EventTrigger(GameEvent.设置玩家摄像机, false);
      EventCenter.Instance.EventTrigger(GameEvent.设置玩家输入状态, false);
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

            EventCenter.Instance.EventTrigger(GameEvent.设置玩家摄像机, true);
            EventCenter.Instance.EventTrigger(GameEvent.设置玩家输入状态, true);
            _npcModle.lookAtCamera.gameObject.SetActive(false);
            EventCenter.Instance.EventTrigger(GameEvent.玩家检测Npc);
            PostProcessingManager.Instance?.AnimateDepthOfFieldTo(10f, 0.6f);
            panel.OnPanelClosed -= onClose;
         };

         panel.OnPanelClosed += onClose;
      });
   }
}
