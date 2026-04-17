using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.FullName)));

        // Identity
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        // JWT settings
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"appsettings に '{JwtSettings.SectionName}' セクションがありません。");

        if (jwtSettings.Secret.Length < 32)
            throw new InvalidOperationException(
                "JwtSettings:Secret は32文字以上にしてください。" +
                "開発環境では dotnet user-secrets、本番環境では環境変数で設定してください。");

        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // false にすることで "sub" などの RFC 7519 クレーム名をそのまま保持する。
                // true (デフォルト) にすると "sub" が ClaimTypes.NameIdentifier に変換されるため、
                // AppClaimTypes.UserId ("sub") での検索が失敗する原因になる。
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    ValidAudience            = jwtSettings.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew                = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        // Application interfaces → Infrastructure implementations
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();

        return services;
    }
}
