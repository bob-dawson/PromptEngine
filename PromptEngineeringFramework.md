
# Prompt Engineering Framework for C# Agent Frameworks

## 项目简介

本项目旨在为 C# 生态下的 Agent / LLM 开发提供**工程化的 Prompt 管理框架**，解决 Prompt 模板在开发、测试和生产环境中的一致性、安全性和可维护性问题。  

通过结合 **编译期静态校验**、**运行时动态校验**以及**编辑器可用变量提示**，显著提升 Prompt 开发效率和生产安全性，同时方便与 Microsoft Agent Framework集成。

---

## 1️ 解决的问题

1. **模板与上下文不一致问题**
   - 现有库通常在运行时解析模板，拼写错误或上下文结构变更可能导致生产环境错误。
   - 目标：在编译期发现模板变量与上下文类不匹配。

2. **运行时安全检查**
   - Prompt 文件可能被修改或新增，生产环境需要动态验证模板与上下文一致性，对于用到错误变量，给出告警，对于没有用到的变量，给出提示。
   
3. **编辑器智能提示**
   - 在开发和编辑阶段，提示模板中可用的上下文变量，方便团队协作和 Prompt 优化，对于用到错误变量，给出告警，禁止保存生效。对于没有用到的变量，给出提示。

4. **工程化管理**
   - 支持模板集中管理、版本控制和多 Agent / 多模型复用。
   
---

## 2️ 核心实现要求

### 2.1 模板管理

- 采用 `.prompt.txt` 或 `.prompty` 文件存储 Prompt 模板。
- 模板中用 `{变量名}` 作为占位符。
- 支持多语言、多模型模板。

### 2.2 上下文强类型绑定

- 为每个模板定义对应 `TContext` 类。
- 类属性对应模板占位符。
- 编译期使用 **Source Generator** 校验模板引用的所有变量是否存在上下文类。

### 2.3 Source Generator 功能

- 扫描项目中带 `[PromptContext("路径")]` 属性的上下文类。
- 解析对应模板文件，提取占位符 `{变量}`。
- 检查占位符是否匹配上下文类属性，报 `Diagnostic` 编译错误。
- 自动生成强类型 Prompt 构建类，例如：

```csharp
public static partial class SummarizePrompt
{
    public static string Build(SummarizeContext context)
        => $"Summarize the following text for user {context.UserName}:
{context.InputText}";
}
```

- 输出运行时元数据文件（JSON），包含模板文件路径、上下文类型、占位符列表。

### 2.4 运行时校验

- 框架提供运行时工具，扫描所有模板及生成的 meta 文件。
- 校验模板占位符与上下文属性一致性。
- 可在应用启动或 CI/CD 阶段执行。

### 2.5 编辑器提示支持

- 根据上下文类生成可用变量列表：
  - JSON 元数据文件：编辑器读取显示可用占位符。
  - 可选输出 TypeScript 类型文件 (`.d.ts`) 用于 Web 编辑器自动提示。

### 2.6 Agent Framework 集成

- 与 Microsoft Agent Framework / Semantic Kernel 兼容。
- 提供简单接口注册 Prompt 构建器与上下文。
- 支持多 Agent Step / 多模型复用。

---

## 3️ 测试要求

### 3.1 单元测试

- 测试模板占位符解析正确性。
- 测试 Source Generator 静态校验：
  - 占位符匹配上下文属性 → 通过。
  - 占位符不匹配上下文属性 → 编译报错。
- 测试生成的 Prompt 构建类输出正确。

### 3.2 运行时验证测试

- 测试运行时动态校验：
  - 模板文件未修改 → 校验通过。
  - 模板文件占位符与上下文不一致 → 抛异常。

### 3.3 编辑器提示测试

- 生成的可用变量 JSON / TypeScript 类型与上下文类一致。
- 在编辑器中可正确显示可选占位符。

### 3.4 CI/CD 集成测试

- 在 CI/CD 阶段执行运行时校验。
- 模板或上下文变更 → 检测异常并阻止部署。

---

## 4️ NuGet 发布要求

1. **包结构**
   ```
   PromptEngine/
     src/
       PromptEngine.Core/   # Source Generator、运行时校验
       PromptEngine.Editor/ # 编辑器提示生成器
       PromptEngine.Agent/  # 与 Agent Framework 集成
   ```
2. **目标框架**
   - `.NET 7` 或 `.NET 8`
   - 支持 SDK 风格项目

3. **依赖**
   - `Microsoft.CodeAnalysis.CSharp`（用于 Source Generator）
   - `System.Text.Json`（用于元数据序列化）
   - 可选：`Scriban` 或其他模板库（用于复杂模板处理）

4. **发布策略**
   - 使用 GitHub Actions 自动生成 NuGet 包。
   - 包含 `.symbols.nupkg` 以支持 IDE 调试。
   - 版本管理遵循 SemVer。

5. **文档与示例**
   - 提供 README、示例 Agent 项目。
   - 说明模板管理、静态校验、运行时校验及编辑器提示生成流程。

---

## 5️ 未来扩展方向

- 支持嵌套模板、条件模板。
- 集成多模型管理，自动切换 Prompt。
- CLI 工具支持模板快速生成、批量校验。
- 支持 VSCode / JetBrains 插件，增强编辑器智能提示。
