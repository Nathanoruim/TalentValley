using System.Globalization;

namespace SqliteToPostgresMigrator;

internal enum ValueProblem
{
    None,
    NullInRequiredColumn,
    ExceedsMaxLength,
    InvalidFormat
}

// Converts SQLite storage values (what EF Core wrote there) to the CLR types Npgsql sends to the target column.
internal static class ValueConverter
{
    private static readonly string[] SupportedTypes =
        ["uuid", "boolean", "integer", "timestamp with time zone", "date", "text", "character varying"];

    public static bool IsSupported(string dataType) => SupportedTypes.Contains(dataType, StringComparer.Ordinal);

    // Never reports the offending value: it can be personal data (S9).
    public static ValueProblem TryConvert(object raw, TargetColumn column, out object? value)
    {
        value = null;
        if (raw is DBNull)
            return column.IsNullable ? ValueProblem.None : ValueProblem.NullInRequiredColumn;

        switch (column.DataType)
        {
            case "uuid":
                return raw is string guid && Guid.TryParse(guid, out var parsedGuid)
                    ? Accept(parsedGuid, out value) : ValueProblem.InvalidFormat;
            case "boolean":
                return raw is long flag && flag is 0 or 1
                    ? Accept(flag == 1, out value) : ValueProblem.InvalidFormat;
            case "integer":
                return raw is long number && number is >= int.MinValue and <= int.MaxValue
                    ? Accept((int)number, out value) : ValueProblem.InvalidFormat;
            case "timestamp with time zone":
                // Values without an offset (the old Auditoria column) were already UTC.
                return raw is string stamp && DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var offset)
                    ? Accept(offset.UtcDateTime, out value) : ValueProblem.InvalidFormat;
            case "date":
                return raw is string day && DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)
                    ? Accept(date, out value) : ValueProblem.InvalidFormat;
            case "text" or "character varying":
                return ConvertText(raw, column, out value);
            default:
                return ValueProblem.InvalidFormat;
        }
    }

    private static ValueProblem ConvertText(object raw, TargetColumn column, out object? value)
    {
        value = null;
        // PostgreSQL text cannot hold NUL characters.
        if (raw is not string text || text.Contains('\0')) return ValueProblem.InvalidFormat;
        // PostgreSQL counts characters (code points), not UTF-16 units; only count precisely when it could matter.
        if (column.MaxLength is { } max && text.Length > max && text.EnumerateRunes().Count() > max)
            return ValueProblem.ExceedsMaxLength;
        return Accept(text, out value);
    }

    private static ValueProblem Accept(object converted, out object? value)
    {
        value = converted;
        return ValueProblem.None;
    }
}
