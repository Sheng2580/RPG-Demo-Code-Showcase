using UnityEngine;

public class CombatContext
{
    // 当前玩家控制器，提供状态切换、锁敌、命中检测等玩家公共能力。
    public PlayerContorller Player { get; }

    // 当前玩家战斗属性，提供攻击力、暴击、能量和后续 Buff 计算。
    public PlayerCombatStats Stats { get; }

    // 当前玩家的 CharacterController，用于形态移动和位移。
    public CharacterController CharacterController { get; }

    // 当前玩家模型上的 Animator，用于形态播放动画和读取动画状态。
    public Animator Animator { get; }

    // 当前玩家 Transform，用于位置、朝向和特效生成。
    public Transform Transform { get; }

    // 当前是否正在锁定敌人。
    public bool IsLockingEnemy => Player != null && Player.isLockingEnemy;

    // 当前锁定的敌人目标。
    public Transform LockEnemyTarget => Player != null ? Player.lockEnemyTarget : null;

    // 玩家配置的敌人 LayerMask，用于攻击判定。
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
