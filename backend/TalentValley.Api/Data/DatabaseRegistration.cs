using Microsoft.EntityFrameworkCore;

namespace TalentValley.Api.Data;

public static class DatabaseRegistration
{
    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
