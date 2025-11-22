using System.ComponentModel.DataAnnotations;

namespace Rest.Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Range(1, 240)] public int ExpirationHours { get; set; } = 2;
}