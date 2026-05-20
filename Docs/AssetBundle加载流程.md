# AssetBundle 加载流程

## 核心类

`Scripts/Framework/ABManager.cs` 是 AssetBundle 加载入口，负责主包加载、Manifest 读取、依赖包加载、资源同步加载和异步加载。

## 加载流程

1. 根据平台选择主包名称：PC、Android 或 iOS。
2. 从 `Application.streamingAssetsPath` 加载主 AssetBundle。
3. 从主包中读取 `AssetBundleManifest`。
4. 加载目标包前，先通过 Manifest 查询并加载全部依赖包。
5. 目标包加载完成后，按资源名和类型读取资源。
6. 如果资源是 `GameObject` Prefab，则实例化后返回；其他资源直接返回。

## 异步策略

`ABManager` 使用协程处理异步加载，并维护正在加载的回调列表。多个系统同时请求同一个包时，加载请求会合并，避免重复加载同一个 AssetBundle。

## 与 UI 的关系

`UIManager` 默认从名为 `uipanel` 的 AssetBundle 中加载 UI 面板。面板加载完成后会挂到对应 UI 层级，并记录到已打开面板表中。关闭面板时不立即销毁，而是回收到 UI 面板池，下一次打开同类面板时直接复用。

## 设计收益

- Manifest 统一管理依赖，减少手工维护资源加载顺序。
- 同步和异步接口并存，兼容不同调用场景。
- 加载中请求合并，降低重复 I/O。
- Prefab 自动实例化，调用方拿到的就是可直接使用的对象。
