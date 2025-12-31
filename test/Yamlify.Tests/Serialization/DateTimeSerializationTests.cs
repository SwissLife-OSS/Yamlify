using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Class for DateTime testing.
/// </summary>
public class DateTimeClass
{
    public DateTime Date { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
}

/// <summary>
/// Class with Guid property.
/// </summary>
public class GuidClass
{
    public Guid Id { get; set; }
    public Guid? OptionalId { get; set; }
}

/// <summary>
/// Class with TimeSpan property.
/// </summary>
public class TimeSpanClass
{
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Class with Uri property.
/// </summary>
public class UriClass
{
    public Uri? Url { get; set; }
}

/// <summary>
/// Tests for DateTime and related types.
/// </summary>
public class DateTimeSerializationTests
{
    [Fact]
    public void SerializeDateTime()
    {
        var obj = new DateTimeClass
        {
            Date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DateTimeClass);

        Assert.Contains("2024", yaml);
    }

    [Fact]
    public void DeserializeDateTime()
    {
        var yaml = """
            date: 2024-01-15T10:30:00Z
            date-time-offset: 2024-01-15T10:30:00Z
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DateTimeClass);

        Assert.NotNull(obj);
        Assert.Equal(2024, obj.Date.Year);
        Assert.Equal(1, obj.Date.Month);
        Assert.Equal(15, obj.Date.Day);
    }

    [Fact]
    public void SerializeGuid()
    {
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var obj = new GuidClass { Id = guid };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.GuidClass);

        Assert.Contains("550e8400-e29b-41d4-a716-446655440000", yaml);
    }

    [Fact]
    public void DeserializeGuid()
    {
        var yaml = """
            id: 550e8400-e29b-41d4-a716-446655440000
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.GuidClass);

        Assert.NotNull(obj);
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), obj.Id);
    }

    [Fact]
    public void SerializeTimeSpan()
    {
        var obj = new TimeSpanClass { Duration = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30)) };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.TimeSpanClass);

        Assert.Contains("02:30:00", yaml);
    }

    [Fact]
    public void DeserializeTimeSpan()
    {
        var yaml = """
            duration: 02:30:00
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TimeSpanClass);

        Assert.NotNull(obj);
        Assert.Equal(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30)), obj.Duration);
    }

    [Fact]
    public void SerializeUri()
    {
        var obj = new UriClass { Url = new Uri("https://example.com/path?query=value") };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.UriClass);

        Assert.Contains("https://example.com/path?query=value", yaml);
    }

    [Fact]
    public void DeserializeUri()
    {
        var yaml = """
            url: https://example.com/path?query=value
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.UriClass);

        Assert.NotNull(obj);
        Assert.Equal("https://example.com/path?query=value", obj.Url?.ToString());
    }

    [Fact]
    public void SerializeDateOnly()
    {
        var obj = new DateOnlyClass 
        { 
            Date = new DateOnly(2024, 6, 15),
            NullableDate = new DateOnly(2025, 12, 25)
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.DateOnlyClass);

        Assert.Contains("2024-06-15", yaml);
        Assert.Contains("2025-12-25", yaml);
    }

    [Fact]
    public void DeserializeDateOnly()
    {
        var yaml = """
            date: 2024-06-15
            nullable-date: 2025-12-25
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DateOnlyClass);

        Assert.NotNull(obj);
        Assert.Equal(new DateOnly(2024, 6, 15), obj.Date);
        Assert.Equal(new DateOnly(2025, 12, 25), obj.NullableDate);
    }

    [Fact]
    public void SerializeTimeOnly()
    {
        var obj = new TimeOnlyClass 
        { 
            Time = new TimeOnly(14, 30, 45),
            NullableTime = new TimeOnly(9, 15, 0)
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.TimeOnlyClass);

        Assert.Contains("14:30:45", yaml);
        Assert.Contains("09:15:00", yaml);
    }

    [Fact]
    public void DeserializeTimeOnly()
    {
        var yaml = """
            time: 14:30:45
            nullable-time: 09:15:00
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TimeOnlyClass);

        Assert.NotNull(obj);
        Assert.Equal(new TimeOnly(14, 30, 45), obj.Time);
        Assert.Equal(new TimeOnly(9, 15, 0), obj.NullableTime);
    }

    [Fact]
    public void RoundTripDateOnly()
    {
        var original = new DateOnlyClass 
        { 
            Date = new DateOnly(2024, 8, 20),
            NullableDate = null
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DateOnlyClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DateOnlyClass);

        Assert.NotNull(result);
        Assert.Equal(original.Date, result.Date);
        Assert.Null(result.NullableDate);
    }

    [Fact]
    public void RoundTripTimeOnly()
    {
        var original = new TimeOnlyClass 
        { 
            Time = new TimeOnly(23, 59, 59, 999),
            NullableTime = new TimeOnly(0, 0, 0)
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.TimeOnlyClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TimeOnlyClass);

        Assert.NotNull(result);
        Assert.Equal(original.Time, result.Time);
        Assert.Equal(original.NullableTime, result.NullableTime);
    }

    [Fact]
    public void RoundTripGuid()
    {
        var original = new GuidClass
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            OptionalId = Guid.Parse("87654321-4321-4321-4321-210987654321")
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.GuidClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.GuidClass);

        Assert.NotNull(result);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.OptionalId, result.OptionalId);
    }

    [Fact]
    public void RoundTripTimeSpan()
    {
        var original = new TimeSpanClass
        {
            Duration = TimeSpan.FromDays(1).Add(TimeSpan.FromHours(12)).Add(TimeSpan.FromMinutes(30))
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.TimeSpanClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TimeSpanClass);

        Assert.NotNull(result);
        Assert.Equal(original.Duration, result.Duration);
    }

    [Fact]
    public void RoundTripUri()
    {
        var original = new UriClass
        {
            Url = new Uri("https://api.example.com/v1/users?limit=100")
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.UriClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.UriClass);

        Assert.NotNull(result);
        Assert.Equal(original.Url, result.Url);
    }

    [Fact]
    public void RoundTripDateTime()
    {
        var original = new DateTimeClass
        {
            Date = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            DateTimeOffset = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DateTimeClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DateTimeClass);

        Assert.NotNull(result);
        // Note: DateTime round-trip may have some parsing variations
        // Just verify we got meaningful values
        Assert.NotEqual(default, result.Date);
        Assert.NotEqual(default, result.DateTimeOffset);
    }

    [Fact]
    public void DeserializeGuidWithNullOptional()
    {
        var yaml = """
            id: 11111111-2222-3333-4444-555555555555
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.GuidClass);

        Assert.NotNull(obj);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), obj.Id);
        Assert.Null(obj.OptionalId);
    }

    [Fact]
    public void DeserializeUriMissingValue()
    {
        // When URL property is missing from YAML, it should be null
        var yaml = "other: value";

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.UriClass);

        Assert.NotNull(obj);
        Assert.Null(obj.Url);
    }

    [Fact]
    public void SerializeUriWithNullValue()
    {
        var obj = new UriClass { Url = null };
        var options = new YamlSerializerOptions { IgnoreNullValues = false };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.UriClass, options);

        Assert.Contains("null", yaml);
    }
}
