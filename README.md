# RPG Demo Code Showcase

这是从 Unity RPG 个人 3D 动作 RPG Demo中整理出的核心代码展示仓库，重点展示框架管理、战斗形态、角色状态机、敌人 AI、UI 面板与 AssetBundle 资源加载流程。

本仓库只保留便于阅读的源码与说明文档，不包含完整 Unity 工程、场景、模型、音频、材质和生成资源。

## 代码结构

```text
RPG-Demo-Code-Showcase/
├── Docs/
│   ├── 项目架构说明.md
│   ├── 战斗系统设计.md
│   ├── AssetBundle加载流程.md
│   └── UI框架说明.md
└── Scripts/
    ├── Framework/   # 单例、事件、资源、对象池、UI、场景、音频等管理器
    ├── Combat/      # 战斗形态、连招数据、武器逻辑、技能/投射物
    ├── Character/   # 玩家、敌人、NPC、状态机与角色数据
    ├── AI/          # 行为树基础节点与敌人 AI 条件节点
    └── UI/          # BasePanel、HUD、背包、对话、商店、交互面板
```

## 重点模块

- `Scripts/Framework/ABManager.cs`：AssetBundle 主包、Manifest、依赖包、同步/异步资源加载。
- `Scripts/Framework/UIManager.cs`：三层 Canvas、面板异步加载、面板缓存与对象池复用。
- `Scripts/Framework/EventCenter.cs`：全局事件分发，降低系统间直接依赖。
- `Scripts/Framework/PoolMgr.cs`：对象池管理，用于特效、UI、战斗对象复用。
- `Scripts/Combat/CombatForm/CombatFormController.cs`：玩家战斗形态切换、能量消耗、武器挂载和战斗上下文。
- `Scripts/Combat/Weapon/WeaponCombatBase.cs`：武器连招与攻击窗口的公共逻辑。
- `Scripts/Character/Player/PlayerState/`：玩家移动、受击、攻击、技能、变身等状态。
- `Scripts/Character/Enemy/OrdinaryEnemy.cs`：普通敌人的行为树决策和状态切换入口。
- `Scripts/AI/BehaviorTreeBuilde.cs`：链式构建行为树，组合 Selector、Sequence、Decorator 等节点。

## 阅读建议

1. 先看 `Docs/项目架构说明.md`，了解各模块职责和调用关系。
2. 再看 `Docs/战斗系统设计.md`，对应阅读 `Scripts/Combat/CombatForm` 和 `Scripts/Combat/Weapon`。
3. 看 `Scripts/Character/Enemy/OrdinaryEnemy.cs` 与 `Scripts/AI`，理解敌人 AI 如何驱动状态机。
4. 看 `Docs/UI框架说明.md` 与 `Scripts/Framework/UIManager.cs`，理解 UI 加载、分层和复用策略。

## 说明

这些代码来自原 Unity 项目的 `Assets/Scripts` 目录，复制时已排除 `.meta`、场景、Prefab、材质、音频和 ScriptableObject 配置资产。部分脚本依赖 UnityEngine、Cinemachine、Input System 或项目中的 Prefab/配置资源，展示仓库以代码阅读为主，不保证可单独编译运行。
