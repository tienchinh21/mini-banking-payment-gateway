namespace MiniBanking.Infrastructure.Security;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MiniBanking";
    public string Audience { get; set; } = "MiniBanking";
    public int ExpirationHours { get; set; } = 8;
}
