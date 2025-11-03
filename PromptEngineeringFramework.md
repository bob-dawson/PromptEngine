# Prompt Engineering Framework for C# Agent Frameworks

## 项目简介

- 本项目旨在为 C#生态下的 Agent / LLM 开发提供工程化的 Prompt 管理框架，解决 Prompt 模板在开发、测试和生产环境中的一致性、安全性和可维护性问题。
- 通过结合 编译期静态校验、运行时动态校验，显著提升 Prompt 开发效率和生产安全性，同时方便与 Microsoft Agent Framework 集成。

---

## 1 解决的问题

### 1.1 模板与上下文不一致问题
- 现有库通常在运行时解析模板，拼写错误或上下文结构变更可能导致生产环境错误。
- 目标：在编译期发现模板变量与上下文类不匹配。

### 1.2 运行时安全检查
- Prompt 文件可能被修改或新增，生产环境需要动态验证模板与上下文一致性；对于用到的错误变量给出告警，对于未使用的变量给出提示。

## 2 核心实现要求

### 2.1 模板管理

- 采用 `.prompt.md` 文件存储 Prompt 模板（推荐使用 Markdown以便结构化与预览）。
- 模板使用 Mustache语法，变量使用 `{{变量名}}`作为占位符；支持 Section、反转 Section、循环等 Mustache 特性。
- 支持多语言、多模型模板。

### 2.2 上下文强类型绑定

- 为每个模板定义对应 `TContext` 类。
- 类属性对应模板占位符。
- 编译期使用 Source Generator 校验模板引用的所有变量是否存在于上下文类中。

### 2.3 Source Generator 功能

- 扫描项目中带 `[PromptContext("路径")]` 特性的上下文类。
- 解析对应模板文件（Markdown 同样支持），提取 Mustache 占位符 `{{变量}}`。
- 检查占位符是否匹配上下文类属性，不一致时报出 Diagnostic 编译错误。
- 自动生成强类型Prompt扩展类，例如可以这样使用：

```csharp
var summarizeContext = new SummarizeContext
{
    UserName = "Alice",
    InputText = "Artificial Intelligence (AI) is revolutionizing the way we work and live. " +
        "From healthcare to transportation, AI systems are being deployed across various industries. " +
        "Machine learning, a subset of AI, enables computers to learn from data without being explicitly programmed. " +
        "Deep learning, a more advanced form of machine learning, uses neural networks to process complex patterns.",
    MaxWords = "50",
    Instructions = "Focus on the key concepts and applications"
};

string prompt = context.BuildSummarizePrompt();
```

- 输出运行时元数据（C# 注册表），包含模板文件路径、上下文类型、模板开发时的原文。

### 2.4 运行时校验

- 框架提供运行时工具，扫描所有模板及生成的 meta 信息。
- 校验模板占位符与上下文属性一致性。
- 可在应用启动或 CI/CD 阶段执行。

### 2.5 Agent Framework 集成

- 与 Microsoft Agent Framework / Semantic Kernel兼容。
- 自动代码生成实现Prompt构建器与上下文的注册。
- 支持多 Agent Step / 多模型复用。

## 3 测试要求

- 提供单元测试覆盖各项功能。

## 4 NuGet 发布

- 支持NuGet发布

## 5 未来拓展

- 工程化管理：模板集中管理、版本控制和多 Agent / 多模型复用等能力，作为后续扩展方向。
