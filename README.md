# heroliss/EazyBuildPipeline

## 项目用途

面向 Unity 编辑器的旧构建与资源流水线工具集，模块包括 MasterControl、PlayerBuilder、SVNUpdate、AssetPreprocessor、AssetPolice、BundleManager 和 PackageManager。

## 当前状态

- 建议分类：旧 Unity Editor 工具资产包
- 生命周期：Dormant
- 最近 push：2019-02-13T06:15:56Z
- README 扫描状态：Missing

最新 push 为 2019-02-13，长期 dormant。更像旧项目专用工具或冻结资产包，不能独立打开运行。

## 实际运行方式

它不是完整 Unity 工程，没有 ProjectSettings、Packages 或 asmdef。将 Assets/_BuildPipeline/EazyBuildPipeline 导入一个兼容的 Unity 项目后，从 Window/EazyBuildPipeline 菜单尝试打开模块；先补齐 Newtonsoft.Json、AssetBundleManagement2 等外部依赖，并按需要准备 SVN、Shell、Android/iOS 构建工具链。

## 依赖与构建线索

Unity Editor；Newtonsoft.Json；部分 BundleManager 依赖仓库未提供的 AssetBundleManagement2；SVN、Shell、Xcode/Android 工具链视模块而定。部分 JSON 配置有尾逗号，严格 JSON 解析器可能失败。

## 目录与证据

仓库只有 Assets 子树；模块配置位于各 Config/ 目录，未发现完整 Unity 工程元数据。

顶层目录/文件线索：

- `.gitattributes`
- `.gitignore`
- `Assets`

## 已知限制

- 本文档只依据 GitHub 元数据、README、构建文件、入口文件和目录证据整理，未进行完整构建或端到端运行验证。
- 不确定的运行环境、密码、服务器地址和兼容版本不在此处臆测。

## 需要确认

是否还在某个旧 Unity 项目中使用？如果没有，可标为冻结工具资产。

## 后续维护建议

1. 先确认“继续维护、完成冻结、历史保留、归档候选”中的一种状态。
2. 若继续保留，再补充一条可复现的运行验证记录和截图/输出。
3. 若确认完成或废弃，只保留简短历史说明，不需要改代码。

## 证据链接

- 仓库：[heroliss/EazyBuildPipeline](https://github.com/heroliss/EazyBuildPipeline)
- 默认分支：master
- 主要语言：C#
- stars / forks：0 / 1
- 扫描时间：2026-10-11T05:10:08Z
