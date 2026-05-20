using System.Collections.Generic;
using UnityEngine;

public class PlayerTransfiguration : PlayerState
{
    private const float MotorcycleOrbitPushRadius = 2.6f;
    private const float MotorcycleOrbitPushMargin = 0.35f;
    private const float MotorcycleOrbitPushSpeed = 8f;

    private readonly HashSet<EnemyBase> _pushedEnemies = new HashSet<EnemyBase>();
    private bool _hasSwitchedForm;
    private bool _hasActionInvincible;

    public override void Enter()
    {
        _hasSwitchedForm = false;
        _hasActionInvincible = true;
        Player.CancelLockEnemy();
        Player.sitFreeLookCam?.ForceUnlockLookInput();
        Player.BeginActionInvincible();
        ActionPostProcessManager.Instance?.PlayTransfigurationEffect();
        PlayerPnael.SetSceneTransfigurationLayout(true);

        Player.playerTimeLineController.GetPlayerTimeLine("PlayerTransfigurationTimeline", FinishTransfiguration);
        Player.PlayAnimation("PlayerTransfiguration");
        EventCenter.Instance.EventTrigger(
            GameEvent.外描边发光,
            new OutlineGlowEventData(true, new Color(0f, 0.3f, 1f, 1f))
        );
    }

    public override void Update()
    {
        PushEnemiesOutOfMotorcycleOrbit();

        if (CurrAnimationStateTag("CutAttack", out var time) && time > 0.9f)
        {
            FinishTransfiguration();
        }
    }

    public override void Exit()
    {
        EndTimelineInvincible();
    }

    private void PushEnemiesOutOfMotorcycleOrbit()
    {
        if (Player == null || Player.enemyLayerMask.value == 0)
        {
            return;
        }

        Vector3 center = Player.transform.position;
        Collider[] hits = Physics.OverlapSphere(center, MotorcycleOrbitPushRadius, Player.enemyLayerMask, QueryTriggerInteraction.Collide);
        float targetRadius = MotorcycleOrbitPushRadius + MotorcycleOrbitPushMargin;
        Vector3 flatCenter = new Vector3(center.x, 0f, center.z);
        _pushedEnemies.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyBase enemy = hits[i] != null ? hits[i].GetComponentInParent<EnemyBase>() : null;
            if (enemy == null || enemy.isDead || !_pushedEnemies.Add(enemy))
            {
                continue;
            }

            Vector3 enemyPosition = enemy.transform.position;
            Vector3 flatEnemyPosition = new Vector3(enemyPosition.x, 0f, enemyPosition.z);
            Vector3 direction = flatEnemyPosition - flatCenter;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = -Player.transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            float pushDistance = Mathf.Min(targetRadius - direction.magnitude, MotorcycleOrbitPushSpeed * Time.deltaTime);
            if (pushDistance > 0f)
            {
                enemy.MoveByKnockback(direction.normalized, pushDistance);
            }
        }
    }

    private void FinishTransfiguration()
    {
        if (_hasSwitchedForm || Player.currentState != PlayerStateType.Transfiguration)
        {
            return;
        }

        EndTimelineInvincible();
        _hasSwitchedForm = true;
        Player.combatFormController?.ToggleTransformForm();
        Player.ChangeState(PlayerStateType.Idle);
    }

    private void EndTimelineInvincible()
    {
        if (!_hasActionInvincible)
        {
            return;
        }

        _hasActionInvincible = false;
        Player.EndActionInvincible();
    }
}
