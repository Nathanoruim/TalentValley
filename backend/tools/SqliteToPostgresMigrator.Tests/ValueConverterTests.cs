namespace SqliteToPostgresMigrator.Tests;

public sealed class ValueConverterTests
{
    private static TargetColumn Column(string dataType, int? maxLength = null, bool nullable = false) =>
        new("Coluna", dataType, maxLength, nullable, IsIdentity: false);

    [Theory]
    [InlineData("uuid", "0F8FAD5B-D9CB-469F-A165-70867728950E", "None")]
    [InlineData("uuid", "0f8fad5b-d9cb-469f-a165-70867728950e", "None")]
    [InlineData("uuid", "not-a-guid", "InvalidFormat")]
    [InlineData("date", "2026-09-11", "None")]
    [InlineData("date", "11/09/2026", "InvalidFormat")]
    [InlineData("timestamp with time zone", "2026-09-11 12:22:11.1234567+00:00", "None")]
    [InlineData("timestamp with time zone", "2026-09-11 12:22:11", "None")]
    [InlineData("timestamp with time zone", "yesterday", "InvalidFormat")]
    public void Text_values_are_parsed_by_target_type(string dataType, string raw, string expected) =>
        Assert.Equal(Enum.Parse<ValueProblem>(expected), ValueConverter.TryConvert(raw, Column(dataType), out _));

    [Theory]
    [InlineData(0L, false, "None")]
    [InlineData(1L, true, "None")]
    [InlineData(2L, false, "InvalidFormat")]
    public void Booleans_come_from_zero_or_one(long raw, bool expected, string problemName)
    {
        var problem = Enum.Parse<ValueProblem>(problemName);
        Assert.Equal(problem, ValueConverter.TryConvert(raw, Column("boolean"), out var value));
        if (problem == ValueProblem.None) Assert.Equal(expected, value);
    }

    [Fact]
    public void Integers_outside_the_int_range_are_rejected()
    {
        Assert.Equal(ValueProblem.None, ValueConverter.TryConvert((long)int.MaxValue, Column("integer"), out var value));
        Assert.Equal(int.MaxValue, value);
        Assert.Equal(ValueProblem.InvalidFormat, ValueConverter.TryConvert(int.MaxValue + 1L, Column("integer"), out _));
    }

    [Fact]
    public void Offsets_are_normalized_to_utc()
    {
        ValueConverter.TryConvert("2026-09-11 12:22:11-03:00", Column("timestamp with time zone"), out var value);

        var stamp = Assert.IsType<DateTime>(value);
        Assert.Equal(new DateTime(2026, 9, 11, 15, 22, 11, DateTimeKind.Utc), stamp);
        Assert.Equal(DateTimeKind.Utc, stamp.Kind);
    }

    [Fact]
    public void Length_is_counted_in_characters_not_utf16_units()
    {
        var twoEmoji = "😀😀"; // 4 UTF-16 units, 2 characters
        Assert.Equal(ValueProblem.None, ValueConverter.TryConvert(twoEmoji, Column("character varying", 2), out _));
        Assert.Equal(ValueProblem.ExceedsMaxLength, ValueConverter.TryConvert(twoEmoji, Column("character varying", 1), out _));
        Assert.Equal(ValueProblem.None, ValueConverter.TryConvert(new string('a', 5000), Column("text"), out _));
    }

    [Fact]
    public void Nulls_follow_the_column_nullability_and_nul_characters_are_rejected()
    {
        Assert.Equal(ValueProblem.None, ValueConverter.TryConvert(DBNull.Value, Column("text", nullable: true), out var value));
        Assert.Null(value);
        Assert.Equal(ValueProblem.NullInRequiredColumn, ValueConverter.TryConvert(DBNull.Value, Column("text"), out _));
        Assert.Equal(ValueProblem.InvalidFormat, ValueConverter.TryConvert("a\0b", Column("text"), out _));
    }

    [Fact]
    public void Unsupported_types_are_flagged() =>
        Assert.False(ValueConverter.IsSupported("bytea"));
}
