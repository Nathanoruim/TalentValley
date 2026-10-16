namespace SqliteToPostgresMigrator;

// Messages of this type are written by this tool and never contain field values, so they are safe to print.
internal sealed class MigrationAbortedException(string message) : Exception(message);
