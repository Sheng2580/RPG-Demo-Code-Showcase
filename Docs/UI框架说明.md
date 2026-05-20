# UI 框架说明

## 核心类

- `UIManager`：UI 框架入口，负责 Canvas 创建、面板加载、打开、关闭、缓存和复用。
- `BasePanel`：所有 UI 面板的基类，统一生命周期接口。
- `CombatTipPanel`、`PlayerPnael`、`EnemyHudPanel`、`DialoguePanel`、`InteractionPanel`、`merchantPanel`、`SetPanel`：具体业务面板。

## UI 分层

`UIManager` 创建全局 `UI_Root`，并拆出三个子 Canvas：

- `Static`：静态层，适合 HUD、背景类 UI。
- `Dynamic`：普通动态层，适合背包、对话、商店等常规面板。
- `Top`：顶层，适合提示、弹窗和遮罩。

## 面板加载与复用

面板类名与资源名保持一致。默认加载路径是 AssetBundle `uipanel`，也提供从 `Resources/UI` 加载的兼容接口。

异步加载完成后，`UIManager` 会把面板挂到目标 Canvas，调用面板生命周期，并通过回调返回面板实例。关闭面板时，实例会从已打开表移除并放入面板池；再次打开同类面板时，优先从池中取出已有实例。

## 设计收益

- 面板生命周期统一，业务面板只关注自身逻辑。
- 面板加载方式可切换 AssetBundle 或 Resources。
- 加载中回调合并，避免同一面板被重复异步加载。
