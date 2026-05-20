using UnityEngine;

public class CombatContext
{
    public PlayerContorller Player { get; }

    public PlayerCombatStats Stats { get; }

    public CharacterController CharacterController { get; }

    public Animator Animator { get; }

    public Transform Transform { get; }

    public bool IsLockingEnemy => Player != null && Player.isLockingEnemy;

    public Transform LockEnemyTarget => Player != null ? Player.lockEnemyTarget : null;

    public LayerMask EnemyLayerMask => Player != null ? Player.enemyLayerMask : default;

    public CombatContext(PlayerContorller player, PlayerCombatStats stats)
    {
        Player = player;
        Stats = stats;
        CharacterController = player != null ? player.characterController : null;
        Animator = player != null && player.model != null ? player.model.animator : null;
        Transform = player != null ? player.transform : null;
    }
}


