namespace PromptEngine.Core.Runtime;

/// <summary>
/// Result of validating prompt templates
/// </summary>
public class ValidationResult
{
 /// <summary>
 /// Whether all validations passed
 /// </summary>
 public bool IsValid { get; set; } = true;

 /// <summary>
 /// Validation errors
 /// </summary>
 public List<string> Errors { get; set; } = new();

 /// <summary>
 /// Validation warnings
 /// </summary>
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
