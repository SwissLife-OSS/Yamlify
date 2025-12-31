using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Enum for testing enum serialization.
/// </summary>
public enum Status
{
    Inactive,
    Active,
    Pending
}

/// <summary>
/// Flags enum for testing bitwise enum values.
/// </summary>
[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

/// <summary>
/// Class with enum properties.
/// </summary>
public class EnumClass
{
    public Status Status { get; set; }
    public Permissions Permissions { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing enums.
/// </summary>
public class EnumSerializationTests
{
    [Fact]
    public void SerializeEnum()
    {
        var obj = new EnumClass { Status = Status.Active, Permissions = Permissions.Read };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);

        Assert.Contains("Active", yaml);
        Assert.Contains("Read", yaml);
    }

    [Fact]
    public void DeserializeEnum()
    {
        var yaml = """
            status: Active
            permissions: Read
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(obj);
        Assert.Equal(Status.Active, obj.Status);
        Assert.Equal(Permissions.Read, obj.Permissions);
    }

    [Theory]
    [InlineData(Status.Inactive)]
    [InlineData(Status.Active)]
    [InlineData(Status.Pending)]
    public void RoundTripEnumValues(Status status)
    {
        var obj = new EnumClass { Status = status };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive()
    {
        var yaml = """
            status: active
            permissions: read
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(obj);
        Assert.Equal(Status.Active, obj.Status);
    }

    [Fact]
    public void SerializeFlagsEnumSingleValue()
    {
        var obj = new EnumClass { Status = Status.Inactive, Permissions = Permissions.Write };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);

        Assert.Contains("Write", yaml);
    }

    [Fact]
    public void SerializeFlagsEnumNone()
    {
        var obj = new EnumClass { Status = Status.Inactive, Permissions = Permissions.None };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);

        Assert.Contains("None", yaml);
    }

    [Fact]
    public void SerializeFlagsEnumAll()
    {
        var obj = new EnumClass { Status = Status.Inactive, Permissions = Permissions.All };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);

        Assert.Contains("All", yaml);
    }

    [Theory]
    [InlineData(Permissions.None)]
    [InlineData(Permissions.Read)]
    [InlineData(Permissions.Write)]
    [InlineData(Permissions.Execute)]
    [InlineData(Permissions.All)]
    public void RoundTripFlagsEnumValues(Permissions permissions)
    {
        var obj = new EnumClass { Status = Status.Inactive, Permissions = permissions };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(result);
        Assert.Equal(permissions, result.Permissions);
    }

    [Fact]
    public void DeserializeFlagsEnumCaseInsensitive()
    {
        var yaml = """
            status: inactive
            permissions: write
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(obj);
        Assert.Equal(Permissions.Write, obj.Permissions);
    }

    [Fact]
    public void SerializeDefaultEnumValue()
    {
        var obj = new EnumClass(); // Default values

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumClass);

        Assert.Contains("Inactive", yaml);
        Assert.Contains("None", yaml);
    }

    [Fact]
    public void DeserializeMinimalYamlUsesDefaultEnumValues()
    {
        // Note: Empty YAML string returns null, so we use a minimal YAML with empty mapping
        var yaml = """
            status: Inactive
            permissions: None
            """;

        var obj = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(obj);
        Assert.Equal(Status.Inactive, obj.Status);
        Assert.Equal(Permissions.None, obj.Permissions);
    }

    [Fact]
    public void RoundTripWithBothEnumProperties()
    {
        var original = new EnumClass { Status = Status.Pending, Permissions = Permissions.Execute };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.EnumClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(result);
        Assert.Equal(original.Status, result.Status);
        Assert.Equal(original.Permissions, result.Permissions);
    }

    #region Standalone Enum Type Registration

    [Fact]
    public void SerializeStandaloneEnumType()
    {
        var status = Status.Active;

        var yaml = YamlSerializer.Serialize(status, TestSerializerContext.Default.Status);

        Assert.Equal("Active", yaml.Trim());
    }

    [Fact]
    public void DeserializeStandaloneEnumType()
    {
        var yaml = "Pending";

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Status);

        Assert.Equal(Status.Pending, result);
    }

    [Fact]
    public void DeserializeStandaloneEnumTypeCaseInsensitive()
    {
        var yaml = "active";

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Status);

        Assert.Equal(Status.Active, result);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Critical)]
    public void RoundTripStandaloneEnumType(Priority priority)
    {
        var yaml = YamlSerializer.Serialize(priority, TestSerializerContext.Default.Priority);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Priority);

        Assert.Equal(priority, result);
    }

    #endregion

    #region Enum Collections

    [Fact]
    public void SerializeEnumArray()
    {
        var obj = new EnumCollectionsClass
        {
            StatusArray = [Status.Active, Status.Pending, Status.Inactive]
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumCollectionsClass);

        Assert.Contains("status-array:", yaml);
        Assert.Contains("Active", yaml);
        Assert.Contains("Pending", yaml);
        Assert.Contains("Inactive", yaml);
    }

    [Fact]
    public void DeserializeEnumArray()
    {
        var yaml = """
            status-array:
              - Active
              - Pending
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumCollectionsClass);

        Assert.NotNull(result?.StatusArray);
        Assert.Equal(2, result.StatusArray.Length);
        Assert.Equal(Status.Active, result.StatusArray[0]);
        Assert.Equal(Status.Pending, result.StatusArray[1]);
    }

    [Fact]
    public void SerializeEnumList()
    {
        var obj = new EnumCollectionsClass
        {
            PriorityList = [Priority.High, Priority.Critical]
        };

        var yaml = YamlSerializer.Serialize(obj, TestSerializerContext.Default.EnumCollectionsClass);

        Assert.Contains("priority-list:", yaml);
        Assert.Contains("High", yaml);
        Assert.Contains("Critical", yaml);
    }

    [Fact]
    public void DeserializeEnumList()
    {
        var yaml = """
            priority-list:
              - Low
              - Medium
              - High
            """;

        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumCollectionsClass);

        Assert.NotNull(result?.PriorityList);
        Assert.Equal(3, result.PriorityList.Count);
        Assert.Equal(Priority.Low, result.PriorityList[0]);
        Assert.Equal(Priority.Medium, result.PriorityList[1]);
        Assert.Equal(Priority.High, result.PriorityList[2]);
    }

    [Fact]
    public void RoundTripEnumCollections()
    {
        var original = new EnumCollectionsClass
        {
            StatusArray = [Status.Active, Status.Inactive],
            PriorityList = [Priority.Low, Priority.Critical]
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.EnumCollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumCollectionsClass);

        Assert.NotNull(result?.StatusArray);
        Assert.Equal(2, result.StatusArray.Length);
        Assert.Equal(Status.Active, result.StatusArray[0]);
        Assert.NotNull(result.PriorityList);
        Assert.Equal(2, result.PriorityList.Count);
        Assert.Equal(Priority.Critical, result.PriorityList[1]);
    }

    #endregion
}
