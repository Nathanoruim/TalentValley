namespace SqliteToPostgresMigrator;

internal static class Sql
{
    // Identifiers always come from the database catalogs, never from user input; quoting keeps mixed-case names valid.
    public static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
