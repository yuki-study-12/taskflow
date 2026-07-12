using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskFlow.Infrastructure.Persistence;

// dotnet ef migrations 実行時のみ使用される。アプリ起動時には使われない。
// `dotnet ef ... --startup-project TaskFlow.WebAPI` を前提に、TaskFlow.WebAPI の
// appsettings.json / user-secrets から接続文字列を読み込み、実行時アプリと同じ
// データベースを指すようにする。
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets("taskflow-webapi-dev")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection の接続文字列が見つかりません。" +
                "'dotnet ef ... --startup-project TaskFlow.WebAPI' の形式でリポジトリルートから実行してください。");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
