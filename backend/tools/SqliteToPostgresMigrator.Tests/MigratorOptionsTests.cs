namespace SqliteToPostgresMigrator.Tests;

public sealed class MigratorOptionsTests
{
    private const string Secret = "senha-secreta-123";

    private static readonly Dictionary<string, string> ValidEnvironment = new()
    {
        ["TARGET_POSTGRES"] = $"Host=ep-teste.neon.tech;Database=neondb;Username=app;Password={Secret};SSL Mode=VerifyFull",
        ["R2_ACCOUNT_ID"] = "abc123",
        ["R2_BUCKET_NAME"] = "talent-valley-files",
        ["R2_ACCESS_KEY_ID"] = "key-id",
        ["R2_SECRET_ACCESS_KEY"] = Secret
    };

    private static MigratorOptions Parse(string[] args, Dictionary<string, string>? environment = null)
    {
        var values = environment ?? ValidEnvironment;
        return MigratorOptions.Parse(args, name => values.GetValueOrDefault(name));
    }

    [Fact]
    public void Reads_arguments_and_environment()
    {
        var options = Parse(["--sqlite", "railway.db", "--storage-dir", "storage"]);

        Assert.Equal("railway.db", options.SqlitePath);
        Assert.Equal("storage", options.StorageDirectory);
        Assert.Equal("talent-valley-files", options.R2!.BucketName);
        Assert.False(options.SkipDatabase);
    }

    [Fact]
    public void Skipping_a_half_does_not_require_its_settings()
    {
        var databaseOnly = Parse(["--sqlite", "railway.db", "--skip-files"],
            new() { ["TARGET_POSTGRES"] = ValidEnvironment["TARGET_POSTGRES"] });
        var filesOnly = Parse(["--sqlite", "railway.db", "--storage-dir", "s", "--skip-database"],
            ValidEnvironment.Where(x => x.Key != "TARGET_POSTGRES").ToDictionary());

        Assert.Null(databaseOnly.R2);
        Assert.Null(filesOnly.TargetPostgres);
    }

    [Theory]
    [InlineData("--storage-dir", "s")]
    [InlineData("--sqlite")]
    [InlineData("--sqlite", "a.db", "--unknown")]
    [InlineData("--sqlite", "a.db", "--skip-files", "--skip-database")]
    [InlineData("--sqlite", "a.db")]
    public void Rejects_incomplete_or_unknown_arguments(params string[] args) =>
        Assert.Throws<MigrationAbortedException>(() => Parse(args));

    [Fact]
    public void A_remote_database_requires_verified_tls()
    {
        var environment = new Dictionary<string, string>(ValidEnvironment)
        {
            ["TARGET_POSTGRES"] = $"Host=ep-teste.neon.tech;Database=neondb;Username=app;Password={Secret};SSL Mode=Require"
        };

        var exception = Assert.Throws<MigrationAbortedException>(
            () => Parse(["--sqlite", "a.db", "--storage-dir", "s"], environment));

        Assert.Contains("VerifyFull", exception.Message);
        Assert.DoesNotContain(Secret, exception.Message);
    }

    [Fact]
    public void Localhost_does_not_require_tls()
    {
        var environment = new Dictionary<string, string>(ValidEnvironment)
        {
            ["TARGET_POSTGRES"] = $"Host=localhost;Username=postgres;Password={Secret}"
        };

        Assert.NotNull(Parse(["--sqlite", "a.db", "--storage-dir", "s"], environment).TargetPostgres);
    }

    [Fact]
    public void Invalid_settings_never_echo_credentials()
    {
        var malformed = new Dictionary<string, string>(ValidEnvironment) { ["TARGET_POSTGRES"] = $"Password={Secret};Bogus=1" };
        var noBucket = new Dictionary<string, string>(ValidEnvironment) { ["R2_BUCKET_NAME"] = "" };

        var first = Assert.Throws<MigrationAbortedException>(() => Parse(["--sqlite", "a.db", "--storage-dir", "s"], malformed));
        var second = Assert.Throws<MigrationAbortedException>(() => Parse(["--sqlite", "a.db", "--storage-dir", "s"], noBucket));

        Assert.DoesNotContain(Secret, first.Message);
        Assert.DoesNotContain(Secret, second.Message);
    }
}
