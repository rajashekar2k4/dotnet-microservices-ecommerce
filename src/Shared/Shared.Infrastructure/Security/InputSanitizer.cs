using System.Text.RegularExpressions;

namespace ECommerce.Shared.Infrastructure.Security;

/// <summary>
/// Input sanitization service for SQL injection and XSS prevention (Security best practice).
/// Follows Single Responsibility Principle - sanitizes input only.
/// </summary>
public interface IInputSanitizer
{
    string SanitizeString(string input);
    string SanitizeHtml(string input);
    bool IsValidEmail(string email);
    bool ContainsSqlInjection(string input);
}

public class InputSanitizer : IInputSanitizer
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
        "EXEC", "EXECUTE", "UNION", "DECLARE", "CAST", "CONVERT", "--", "/*", "*/"
    };

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Sanitizes string input to prevent XSS (KISS principle - simple and effective).
    /// </summary>
    public string SanitizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove potentially dangerous characters
        input = input.Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#x27;")
                    .Replace("/", "&#x2F;");

        return input.Trim();
    }

    /// <summary>
    /// Removes HTML tags from input (Security best practice).
    /// </summary>
    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return HtmlTagRegex.Replace(input, string.Empty);
    }

    /// <summary>
    /// Validates email format (KISS principle).
    /// </summary>
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Checks for SQL injection patterns (Security best practice).
    /// </summary>
    public bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var upperInput = input.ToUpperInvariant();

        return SqlKeywords.Any(keyword => upperInput.Contains(keyword));
    }
}
