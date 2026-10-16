using Npgsql;
using SqliteToPostgresMigrator;
using TalentValley.Api.Storage;

// Exit codes: 0 = copied and conferred, 1 = aborted (nothing half-written in the database), 2 = copied with warnings.
return await Run(args);

static async Task<int> Run(string[] args)
{
    if (args.Contains("--help") || args.Contains("-h"))
    {
        Console.WriteLine(MigratorOptions.Usage);
        return 0;
    }

    try
    {
        var options = MigratorOptions.Parse(args, Environment.GetEnvironmentVariable);
        var warnings = false;

        // Files first: re-sending is harmless, whereas the database copy only runs against an empty target.
        if (!options.SkipFiles)
        {
            using var client = R2FileStorage.CreateClient(options.R2!);
            var store = new S3ObjectStore(client, options.R2!.BucketName!);
            var report = await FileCopier.CopyAsync(
                options.SqlitePath, options.StorageDirectory!, store, Console.WriteLine, CancellationToken.None);
            PrintFiles(report);
            warnings |= report.HasWarnings;
        }

        if (!options.SkipDatabase)
        {
            var counts = await DatabaseCopier.CopyAsync(
                options.SqlitePath, options.TargetPostgres!, Console.WriteLine, CancellationToken.None);
            PrintDatabase(counts);
        }

        return warnings ? 2 : 0;
    }
    catch (MigrationAbortedException exception)
    {
        Console.Error.WriteLine($"ABORTADO: {exception.Message}");
        return 1;
    }
    catch (Exception exception)
    {
        // Driver messages can quote row values or credentials, so only the kind of failure is shown.
        var detail = exception is PostgresException postgres ? $" (SqlState {postgres.SqlState})" : string.Empty;
        Console.Error.WriteLine($"ERRO inesperado: {exception.GetType().Name}{detail}. Detalhes omitidos para não expor dados pessoais.");
        return 1;
    }
}

static void PrintDatabase(IReadOnlyList<TableCount> counts)
{
    Console.WriteLine();
    Console.WriteLine("Banco de dados (origem x destino):");
    foreach (var count in counts)
        Console.WriteLine($"  {count.Table,-28} {count.Source,8} {count.Target,8}  {(count.Source == count.Target ? "ok" : "DIFERENTE")}");
    Console.WriteLine($"  Total de linhas copiadas: {counts.Sum(x => x.Target)}");
}

static void PrintFiles(FileReport report)
{
    Console.WriteLine();
    Console.WriteLine("Arquivos:");
    Console.WriteLine($"  Arquivos válidos na pasta de origem:        {report.LocalFiles}");
    Console.WriteLine($"  Enviados agora / já estavam no bucket:      {report.Uploaded} / {report.AlreadyInBucket}");
    Console.WriteLine($"  Objetos no bucket (pastas fotos, curriculos, certificados): {report.BucketObjects}");
    Console.WriteLine($"  Chaves referenciadas pelo banco:            {report.ReferencedKeys}");
    Console.WriteLine($"  Aviso - referenciadas sem arquivo na origem: {report.ReferencedWithoutLocalFile}");
    Console.WriteLine($"  Aviso - arquivos sem referência no banco:    {report.LocalFilesWithoutReference}");
    Console.WriteLine($"  Aviso - arquivos com nome inválido ignorados: {report.IgnoredFiles}");
    Console.WriteLine($"  Aviso - referências com chave inválida:      {report.InvalidReferences}");
    Console.WriteLine($"  Informativo - objetos no bucket que não vieram da origem: {report.BucketObjectsNotInSource}");
}
