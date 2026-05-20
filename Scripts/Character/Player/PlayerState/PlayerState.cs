using UnityEngine;

public class PlayerState : StateBase
{
    private float _rotationAngle;
    private Transform _mainCamera;
    private float _angleVelocity = 0f;
    protected PlayerContorller Player;
    private float _camYaw;
    
    public override void Init(IStateMachineOwner owner)
   {
      base.Init(owner);
      Player=(PlayerContorller)owner;
      _mainCamera= Camera.main.transform;
   }
    
   protected void MoldRotate()
   {
      // ====================== 你的原有代码 一行不改 ======================
      Vector2 input = GameInputManger.Instance.Movement;
      Vector3 inputDir = new Vector3(input.x, 0f, input.y);
      if (inputDir.sqrMagnitude < 0.0001f) return;

      Transform camTransform = _mainCamera.transform;
      Vector3 camForward = camTransform.forward;
      camForward.y = 0; 
      camForward.Normalize();

      Vector3 camRight = camTransform.right;
      camRight.y = 0;
      camRight.Normalize();

      Vector3 worldDir = camForward * inputDir.z + camRight * inputDir.x;
      worldDir.Normalize();

      float targetAngle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
      
      targetAngle -= Player.sitFreeLookCam.RotateFixAngle; 


      float smoothedY = Mathf.SmoothDampAngle(
         Player.transform.eulerAngles.y,
         targetAngle,
         ref _angleVelocity,
         0.06f,
         Mathf.Infinity,
         Time.unscaledDeltaTime
      );
      Player.transform.eulerAngles = Vector3.up * smoothedY;
   }
   //通过名字判断当前状态 并获得当前状态进行的值
   protected virtual bool CurrAnimationStateName(string stateName , out float normalizedTime ,int layer = 0)
   {
      normalizedTime = 0f;
      if (!TryGetAnimator(out Animator animator))
      {
         return false;
      }

      AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(layer);
      if (nextInfo.IsName(stateName))
      {
         normalizedTime = nextInfo.normalizedTime;
         return true;
      }
      AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
      normalizedTime = info.normalizedTime;
      return info.IsName(stateName);
   }
   
   protected virtual bool CurrAnimationStateName(string stateName ,int layer = 0)
   {
      if (!TryGetAnimator(out Animator animator))
      {
         return false;
      }

      AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(layer);
      if (nextInfo.IsName(stateName))
      {
         return true;
      }
      AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
      return info.IsName(stateName);
   }

   protected virtual bool CurrAnimationStateTag(string tag, out float normalizedTime)
   {
      normalizedTime = 0f;
      if (!TryGetAnimator(out Animator animator))
      {
         return false;
      }

      AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);
      if (nextInfo.IsTag(tag))
      {
         normalizedTime = nextInfo.normalizedTime;
         return true;
      }
      AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
      normalizedTime = info.normalizedTime;
      return info.IsTag(tag);
   }

   protected bool TryGetAnimator(out Animator animator)
   {
      animator = null;
      if (Player == null || Player.model == null || Player.model.animator == null)
      {
         return false;
      }

      animator = Player.model.animator;
      return true;
   }
   
   protected virtual void OnRootMotionAction(Vector3 dir, Quaternion rot)
   {
      Player.characterController.Move(dir);
   }
    
}
