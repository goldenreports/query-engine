using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GoldenReports.QueryEngine.Configuration;

public record SecuritySettings
{
    public CorsSettings Cors { get; init; } = new();
    
    public JwtBearerOptions Jwt { get; init; } = new();
}