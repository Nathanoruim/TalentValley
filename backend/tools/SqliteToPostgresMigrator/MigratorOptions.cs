using Npgsql;
using TalentValley.Api.Storage;

namespace SqliteToPostgresMigrator;

internal sealed record MigratorOptions(
    string SqlitePath,
    string? StorageDirectory,
    string? TargetPostgres,
    R2Options? R2,
    bool SkipDatabase,
    bool SkipFiles)
{
    public const string Usage = """
        Uso: SqliteToPostgresMigrator --sqlite <arquivo.db> [--storage-dir <pasta>] [--skip-database] [--skip-files]

          --sqlite <arquivo>      cópia do banco SQLite da Railway (obrigatório; também fornece as chaves de arquivos)
          --storage-dir <pasta>   cópia de /data/storage, com as subpastas fotos, curriculos e certificados
          --skip-database         não copia o banco (só os arquivos)
          --skip-files            não copia os arquivos (só o banco)

        Variáveis de ambiente (nunca por arquivo versionado):
          TARGET_POSTGRES         connection string do Postgres de destino (SSL Mode=VerifyFull fora do localhost)
          R2_ACCOUNT_ID, R2_BUCKET_NAME, R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY
        """;

    private static readonly string[] LocalHosts = ["localhost", "127.0.0.1", "::1"];

    public static MigratorOptions Parse(string[] args, Func<string, string?> environment)
    {
        string? sqlite = null, storage = null;
        bool skipDatabase = false, skipFiles = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sqlite": sqlite = NextValue(args, ref i); break;
                case "--storage-dir": storage = NextValue(args, ref i); break;
                case "--skip-database": skipDatabase = true; break;
                case "--skip-files": skipFiles = true; break;
                default: throw new MigrationAbortedException($"Argumento desconhecido: {args[i]}.");
            }
        }

        if (string.IsNullOrWhiteSpace(sqlite))
            throw new MigrationAbortedException("Informe --sqlite <arquivo.db>.");
        if (skipDatabase && skipFiles)
            throw new MigrationAbortedException("--skip-database e --skip-files juntos não deixam nada para copiar.");
        if (!skipFiles && string.IsNullOrWhiteSpace(storage))
            throw new MigrationAbortedException("Informe --storage-dir <pasta> ou use --skip-files.");

        var postgres = skipDatabase ? null : RequireTargetPostgres(environment);
        var r2 = skipFiles ? null : ReadR2(environment);
        return new MigratorOptions(sqlite, skipFiles ? null : storage, postgres, r2, skipDatabase, skipFiles);
    }

    private static string NextValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
            throw new MigrationAbortedException($"Falta o valor de {args[index]}.");
        return args[++index];
    }

    private static string RequireTargetPostgres(Func<string, string?> environment)
    {
        var value = environment("TARGET_POSTGRES");
        if (string.IsNullOrWhiteSpace(value))
            throw new MigrationAbortedException("Defina a variável de ambiente TARGET_POSTGRES.");

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(value);
        }
        catch (ArgumentException)
        {
            // The parser message can quote parts of the connection string, so it is not forwarded.
            throw new MigrationAbortedException("TARGET_POSTGRES não é uma connection string válida.");
        }

        if (!LocalHosts.Contains(builder.Host, StringComparer.OrdinalIgnoreCase) && builder.SslMode != SslMode.VerifyFull)
            throw new MigrationAbortedException("TARGET_POSTGRES remoto exige SSL Mode=VerifyFull.");
        return value;
    }

    private static R2Options ReadR2(Func<string, string?> environment)
    {
        var options = new R2Options
        {
            AccountId = environment("R2_ACCOUNT_ID"),
            BucketName = environment("R2_BUCKET_NAME"),
            AccessKeyId = environment("R2_ACCESS_KEY_ID"),
            SecretAccessKey = environment("R2_SECRET_ACCESS_KEY")
        };
        try
        {
            options.Validate();
        }
        catch (InvalidOperationException)
        {
            throw new MigrationAbortedException(
                "R2_ACCOUNT_ID, R2_BUCKET_NAME, R2_ACCESS_KEY_ID e R2_SECRET_ACCESS_KEY precisam estar definidas e válidas.");
        }
        return options;
    }
}
