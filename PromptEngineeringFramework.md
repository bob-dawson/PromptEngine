# Prompt Engineering Framework for C# Agent Frameworks

## 项目简介

本项目旨在为 C#生态下的 Agent / LLM 开发提供工程化的 Prompt 管理框架，解决 Prompt 模板在开发、测试和生产环境中的一致性、安全性和可维护性问题。

通过结合 编译期静态校验、运行时动态校验以及编辑器可用变量提示，显著提升 Prompt 开发效率和生产安全性，同时方便与 Microsoft Agent Framework集成。

---

##1️解决的问题

1. 模板与上下文不一致问题
 -现有库通常在运行时解析模板，拼写错误或上下文结构变更可能导致生产环境错误。
 -目标：在编译期发现模板变量与上下文类不匹配。

2.运行时安全检查
 - Prompt 文件可能被修改或新增，生产环境需要动态验证模板与上下文一致性，对于用到错误变量，给出告警，对于没有用到的变量，给出提示。
 
3. 编辑器智能提示
 - 在开发和编辑阶段，提示模板中可用的上下文变量，方便团队协作和 Prompt 优化，对于用到错误变量，给出告警，禁止保存生效。对于没有用到的变量，给出提示。

4. 工程化管理
 - 支持模板集中管理、版本控制和多 Agent / 多模型复用。
 
---

##2️ 核心实现要求

###2.1 模板管理

-采用 `.prompt.md` 文件存储 Prompt 模板（推荐使用 Markdown以便结构化与预览）。
- 模板中用 `{变量名}`作为占位符。
- 支持多语言、多模型模板。

###2.2 上下文强类型绑定

- 为每个模板定义对应 `TContext` 类。
- 类属性对应模板占位符。
- 编译期使用 Source Generator 校验模板引用的所有变量是否存在上下文类。

###2.3 Source Generator 功能

- 扫描项目中带 `[PromptContext("路径")]` 属性的上下文类。
-解析对应模板文件（Markdown 同样支持），提取占位符 `{变量}`。
- 检查占位符是否匹配上下文类属性，报 Diagnostic 编译错误。
- 自动生成强类型 Prompt 构建类，例如：

```csharp
public static partial class SummarizePromptBuilder
{
 public static string Build(SummarizeContext context)
 => $"# Summarize Request for {context.UserName}\n\n## Input\n\n```text\n{context.InputText}\n```\n\n## Requirements\n- Provide a concise summary in {context.MaxWords} words or less.\n";
}
```

- 输出运行时元数据（C# 注册表），包含模板文件路径、上下文类型、占位符列表与模板原文。

###2.4运行时校验

- 框架提供运行时工具，扫描所有模板及生成的 meta 信息。
- 校验模板占位符与上下文属性一致性。
- 可在应用启动或 CI/CD 阶段执行。

###2.5 Agent Framework 集成

- 与 Microsoft Agent Framework / Semantic Kernel兼容。
- 提供简单接口注册 Prompt 构建器与上下文。
- 支持多 Agent Step / 多模型复用。

---

##3️ 测试要求

- 同步更新测试与示例以适配 `.prompt.md`。

---

##4️ NuGet 发布要求

- 包结构与原设计一致，模板后缀更新为 `.prompt.md` 不影响包形态。

---

4.重新构建并运行 CLI 或运行时校验工具。
