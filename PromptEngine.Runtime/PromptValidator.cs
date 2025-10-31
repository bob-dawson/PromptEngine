using System.Reflection;
using System.Text.Json;
using PromptEngine.Core.Models;

namespace PromptEngine.Runtime;

/// <summary>
///运行时 Prompt 校验器（放在非分析器包中，允许 File/IO）
/// </summary>
public class PromptValidator
{
 private readonly List<PromptMetadata> _metadata = new();

 /// <summary>
 /// 获取已加载的元数据（只读）
 /// </summary>
 public IReadOnlyList<PromptMetadata> GetLoadedMetadata() => _metadata.AsReadOnly();

 /// <summary>
 /// 从元数据文件加载
 /// </summary>
 public void LoadMetadata(string metadataFilePath)
 {
 if (!File.Exists(metadataFilePath))
 {
 throw new FileNotFoundException($"Metadata file not found: {metadataFilePath}");
 }

 var json = File.ReadAllText(metadataFilePath);
 var metadata = JsonSerializer.Deserialize<List<PromptMetadata>>(json);
 if (metadata != null)
 {
 _metadata.AddRange(metadata);
 }
 }

 /// <summary>
 /// 批量追加元数据
 /// </summary>
 public void LoadMetadata(IEnumerable<PromptMetadata> items)
 {
 if (items == null) return;
 _metadata.AddRange(items);
 }

 /// <summary>
 ///通过反射从包含生成元数据注册表的程序集加载
 ///期望类型：PromptEngine.Generated.PromptMetadataRegistry，公开静态属性 All
 /// </summary>
 public void LoadMetadataFromAssembly(string assemblyPath)
 {
 if (!File.Exists(assemblyPath))
 {
 throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
 }

 var asm = Assembly.LoadFrom(assemblyPath);
 var registryType = asm.GetType("PromptEngine.Generated.PromptMetadataRegistry", throwOnError: false, ignoreCase: false);
 if (registryType == null)
 {
 return; // 非目标程序集，忽略
 }

 var allProp = registryType.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
 if (allProp == null)
 {
 return;
 }

 var value = allProp.GetValue(null);
 if (value is IEnumerable<PromptMetadata> items)
 {
 _metadata.AddRange(items);
 }
 }

 /// <summary>
 /// 从目录批量加载元数据（兼容 JSON以及通过扫描程序集加载）
 /// </summary>
 public void LoadMetadataFromDirectory(string directory, string pattern = "*.prompt.meta.json")
 {
 if (!Directory.Exists(directory))
 {
 throw new DirectoryNotFoundException($"Directory not found: {directory}");
 }

 //1)兼容旧版 JSON 元数据
 var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
 foreach (var file in files)
 {
 try
 {
 LoadMetadata(file);
 }
 catch (Exception ex)
 {
 throw new InvalidOperationException($"Failed to load metadata from {file}", ex);
 }
 }

 //2) 尝试从目录内的所有程序集加载注册表
 var assemblies = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories)
 .Concat(Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories));
 foreach (var asmPath in assemblies)
 {
 try
 {
 LoadMetadataFromAssembly(asmPath);
 }
 catch
 {
 // 忽略无法加载的文件
 }
 }
 }

 /// <summary>
 /// 验证所有模板
 /// </summary>
 public ValidationResult ValidateAll()
 {
 var result = new ValidationResult();

 foreach (var meta in _metadata)
 {
 var templateValidation = ValidateTemplate(meta);
 if (!templateValidation.IsValid)
 {
 result.Errors.AddRange(templateValidation.Errors);
 }
 result.Warnings.AddRange(templateValidation.Warnings);
 }

 result.IsValid = result.Errors.Count ==0;
 return result;
 }

 /// <summary>
 /// 验证单个模板
 /// </summary>
 public ValidationResult ValidateTemplate(PromptMetadata metadata)
 {
 var result = new ValidationResult();

 // 检查模板文件是否存在
 if (!File.Exists(metadata.TemplatePath))
 {
 result.Errors.Add($"Template file not found: {metadata.TemplatePath}");
 result.IsValid = false;
 return result;
 }

 //读取模板内容
 var templateContent = File.ReadAllText(metadata.TemplatePath);

 // 提取实际的占位符
 var actualPlaceholders = PromptEngine.Core.Parsers.PromptTemplateParser.ExtractPlaceholders(templateContent);

 // 与元数据中的占位符比较
 var expectedPlaceholders = new HashSet<string>(metadata.Placeholders, StringComparer.OrdinalIgnoreCase);
 var contextProperties = new HashSet<string>(metadata.ContextProperties, StringComparer.OrdinalIgnoreCase);

 var (isValid, missingProperties, unusedProperties) =
 PromptEngine.Core.Parsers.PromptTemplateParser.ValidateTemplate(actualPlaceholders, contextProperties);

 if (!isValid)
 {
 foreach (var missing in missingProperties)
 {
 result.Errors.Add($"Template '{metadata.TemplateName}' uses undefined placeholder: {{{missing}}}");
 }
 }

 foreach (var unused in unusedProperties)
 {
 result.Warnings.Add($"Context property '{unused}' in '{metadata.ContextTypeName}' is not used in template '{metadata.TemplateName}'");
 }

 result.IsValid = result.Errors.Count ==0;
 return result;
 }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
 public bool IsValid { get; set; } = true;
 public List<string> Errors { get; set; } = new();
 public List<string> Warnings { get; set; } = new();

 public override string ToString()
 {
 var sb = new System.Text.StringBuilder();
 sb.AppendLine($"Validation Result: {(IsValid ? "PASSED" : "FAILED")}");

 if (Errors.Count >0)
 {
 sb.AppendLine($"\nErrors ({Errors.Count}):");
 foreach (var error in Errors)
 {
 sb.AppendLine($" - {error}");
 }
 }

 if (Warnings.Count >0)
 {
 sb.AppendLine($"\nWarnings ({Warnings.Count}):");
 foreach (var warning in Warnings)
 {
 sb.AppendLine($" - {warning}");
 }
 }

 return sb.ToString();
 }
}
