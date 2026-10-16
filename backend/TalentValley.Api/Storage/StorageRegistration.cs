namespace TalentValley.Api.Storage;

public static class StorageRegistration
{
    public static void AddFileStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        var options = section.Get<StorageOptions>() ?? new StorageOptions();
        services.Configure<StorageOptions>(section);

        var provider = options.Provider?.Trim();
        if (string.Equals(provider, StorageOptions.R2Provider, StringComparison.OrdinalIgnoreCase))
        {
            options.R2.Validate();
            services.AddSingleton(_ => R2FileStorage.CreateClient(options.R2));
            services.AddSingleton<IFileStorage, R2FileStorage>();
            return;
        }

        if (!string.IsNullOrEmpty(provider) && !string.Equals(provider, StorageOptions.LocalProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage:Provider must be 'R2' or 'Local'.");

        // Private files must never sit on the ephemeral disk of a hosted environment.
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Storage:Provider must be 'R2' outside Development.");

        services.AddSingleton<IFileStorage, LocalFileStorage>();
    }
}
