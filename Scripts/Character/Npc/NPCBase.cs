using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class NPCBase : CharacterBase, IStateMachineOwner, IInteractable
{
   protected Volume GlobalVolume;
   protected StateMachine _stateMachine;

   [HideInInspector]
   public NPCStateType currentState;

   [HideInInspector]
   public NPCStateType previousState;

   public int npcID;
   public GameObject Player;
   public GameObject head;
   
   public Dictionary<string, UnityAction> CurrNpcInteractionActions = new Dictionary<string, UnityAction>();
   public IReadOnlyDictionary<string, UnityAction> InteractionActions => CurrNpcInteractionActions;

   private Vector3 headRef;

   protected virtual void Awake()
   {
      _stateMachine = new StateMachine();
      _stateMachine.Init(this);
   }

   protected override void Start()
   {
      base.Start();
      GameObject volumeObj = GameObject.Find("GlobalVolume");
      if (volumeObj == null)
      {
         volumeObj = GameObject.Find("Global Volume");
      }

      GlobalVolume = volumeObj != null ? volumeObj.GetComponent<Volume>() : null;
      Player = GameManager.Instance != null ? GameManager.Instance.Player : null;
      isEnableGravity = false;
   }

   protected virtual void ChangeState(NPCStateType newState)
   {
      previousState = currentState;
      currentState = newState;
      switch (newState)
      {
         case NPCStateType.idle:
            _stateMachine.ChangeState<NpcIdle>();
            break;
      }
   }

   public void DetectionPlayer()
   {
      float distance = Vector3.Distance(Player.transform.position, transform.position);
      if (distance < 5f)
      {
         Vector3 directionToTarget = Player.transform.position - transform.position;
         float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
         if (Mathf.Abs(angle) < 60)
         {
            head.transform.forward = Vector3.SmoothDamp(head.transform.forward, directionToTarget, ref headRef, 0.1f);
         }
      }
   }

   protected void AddAction(string key, UnityAction action)
   {
      CurrNpcInteractionActions[key] = action;
   }

   protected void OpenDialoguePanel()
   {
      UIManager.Instance.OpenPanelAsync<DialoguePanel>(UILayer.Dynamic, panel =>
      {
         if (panel == null)
         {
            Debug.LogWarning("[NPCBase] 打开 DialoguePanel 失败");
            return;
         }

         panel.OpenDialogue(this);
      });
   }
}
