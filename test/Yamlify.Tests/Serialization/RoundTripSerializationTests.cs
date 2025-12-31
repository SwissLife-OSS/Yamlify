using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Round-trip tests ensuring serialize then deserialize produces equivalent objects.
/// </summary>
public class RoundTripSerializationTests
{
    [Fact]
    public void RoundTripSimpleClass()
    {
        var original = new SimpleClass { Name = "Test", Value = 42, IsActive = true };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Value, result.Value);
        Assert.Equal(original.IsActive, result.IsActive);
    }

    [Fact]
    public void RoundTripNestedClass()
    {
        var original = new ParentClass
        {
            Title = "Parent",
            Child = new SimpleClass { Name = "Child", Value = 10, IsActive = false }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ParentClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ParentClass);

        Assert.NotNull(result);
        Assert.Equal(original.Title, result.Title);
        Assert.NotNull(result.Child);
        Assert.Equal(original.Child.Name, result.Child.Name);
        Assert.Equal(original.Child.Value, result.Child.Value);
    }

    [Fact]
    public void RoundTripList()
    {
        var original = new CollectionsClass
        {
            StringList = new List<string> { "alpha", "beta", "gamma" }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.CollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.StringList);
        Assert.Equal(original.StringList.Count, result.StringList.Count);
        Assert.Equal(original.StringList, result.StringList);
    }

    [Fact]
    public void RoundTripDictionary()
    {
        var original = new CollectionsClass
        {
            StringIntDictionary = new Dictionary<string, int>
            {
                ["one"] = 1,
                ["two"] = 2,
                ["three"] = 3
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.CollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.CollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.StringIntDictionary);
        Assert.Equal(original.StringIntDictionary.Count, result.StringIntDictionary.Count);
        Assert.Equal(1, result.StringIntDictionary["one"]);
        Assert.Equal(2, result.StringIntDictionary["two"]);
        Assert.Equal(3, result.StringIntDictionary["three"]);
    }

    [Fact]
    public void RoundTripStruct()
    {
        var original = new SimpleStruct { X = 100, Y = 200, Label = "Origin" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.SimpleStruct);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.SimpleStruct);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
        Assert.Equal(original.Label, result.Label);
    }

    [Fact]
    public void RoundTripEnum()
    {
        var original = new EnumClass { Status = Status.Active, Permissions = Permissions.Read | Permissions.Write };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.EnumClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.EnumClass);

        Assert.NotNull(result);
        Assert.Equal(original.Status, result.Status);
        Assert.Equal(original.Permissions, result.Permissions);
    }

    [Fact]
    public void RoundTripRecordStruct()
    {
        var original = new PointRecord(3.14, 2.71);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PointRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PointRecord);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
    }

    [Fact]
    public void RoundTripNestedCollections()
    {
        var original = new NestedCollectionsClass
        {
            Matrix = new List<List<int>>
            {
                new() { 1, 2, 3 },
                new() { 4, 5, 6 }
            },
            TagGroups = new Dictionary<string, List<string>>
            {
                ["fruits"] = new() { "apple", "banana" },
                ["colors"] = new() { "red", "blue", "green" }
            }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.NestedCollectionsClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NestedCollectionsClass);

        Assert.NotNull(result);
        Assert.NotNull(result.Matrix);
        Assert.Equal(2, result.Matrix.Count);
        Assert.Equal(original.Matrix[0], result.Matrix[0]);
        Assert.Equal(original.Matrix[1], result.Matrix[1]);
        
        Assert.NotNull(result.TagGroups);
        Assert.Equal(2, result.TagGroups.Count);
        Assert.Equal(original.TagGroups["fruits"], result.TagGroups["fruits"]);
        Assert.Equal(original.TagGroups["colors"], result.TagGroups["colors"]);
    }

    [Fact]
    public void RoundTripPositionalRecord()
    {
        var original = new PositionalRecord("John", "Doe", 42);

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.PositionalRecord);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.PositionalRecord);

        Assert.NotNull(result);
        Assert.Equal(original.FirstName, result.FirstName);
        Assert.Equal(original.LastName, result.LastName);
        Assert.Equal(original.Age, result.Age);
    }

    [Fact]
    public void RoundTripTimeSpan()
    {
        var original = new TimeSpanClass { Duration = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30)) };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.TimeSpanClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.TimeSpanClass);

        Assert.NotNull(result);
        Assert.Equal(original.Duration, result.Duration);
    }

    [Fact]
    public void RoundTripUri()
    {
        var original = new UriClass { Url = new Uri("https://example.com/api/v1?key=value") };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.UriClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.UriClass);

        Assert.NotNull(result);
        Assert.NotNull(result.Url);
        Assert.Equal(original.Url, result.Url);
    }

    [Fact]
    public void RoundTripInheritedClass()
    {
        var original = new Dog { Name = "Buddy", Breed = "Golden Retriever" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.Dog);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Dog);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Breed, result.Breed);
        Assert.Equal(original.Sound, result.Sound);
    }

    [Fact]
    public void RoundTripGuid()
    {
        var original = new GuidClass 
        { 
            Id = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            OptionalId = Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab")
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.GuidClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.GuidClass);

        Assert.NotNull(result);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.OptionalId, result.OptionalId);
    }

    [Fact]
    public void RoundTripNullableTypes()
    {
        var original = new NullableTypesClass
        {
            NullableInt = 42,
            NullableDouble = 3.14,
            NullableBool = true,
            NullableString = "test"
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.NullableTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.NullableTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.NullableInt, result.NullableInt);
        Assert.Equal(original.NullableDouble, result.NullableDouble);
        Assert.Equal(original.NullableBool, result.NullableBool);
        Assert.Equal(original.NullableString, result.NullableString);
    }

    [Fact]
    public void RoundTripImmutablePoint()
    {
        var original = new ImmutablePoint { X = 1.5, Y = 2.5, Z = 3.5 };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.ImmutablePoint);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.ImmutablePoint);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
        Assert.Equal(original.Z, result.Z);
    }

    [Fact]
    public void RoundTripDateOnlyTimeOnly()
    {
        var original = new DateOnlyClass
        {
            Date = new DateOnly(2025, 6, 15),
            NullableDate = new DateOnly(2024, 12, 25)
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.DateOnlyClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.DateOnlyClass);

        Assert.NotNull(result);
        Assert.Equal(original.Date, result.Date);
        Assert.Equal(original.NullableDate, result.NullableDate);
    }

    [Fact]
    public void RoundTripMixedTypesClass()
    {
        var original = new MixedTypesClass
        {
            Name = "CompleteObject",
            Count = 999,
            Ratio = 1.618,
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Id = Guid.NewGuid(),
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Scores = new Dictionary<string, int> { ["score1"] = 100, ["score2"] = 200 },
            Nested = new SimpleClass { Name = "Nested", Value = 50, IsActive = true }
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.MixedTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.MixedTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Count, result.Count);
        Assert.Equal(original.Ratio, result.Ratio);
        Assert.Equal(original.Id, result.Id);
        Assert.NotNull(result.Tags);
        Assert.Equal(original.Tags.Count, result.Tags.Count);
        Assert.NotNull(result.Scores);
        Assert.Equal(original.Scores["score1"], result.Scores["score1"]);
        Assert.NotNull(result.Nested);
        Assert.Equal(original.Nested.Name, result.Nested.Name);
    }

    [Fact]
    public void RoundTripAllNumericTypes()
    {
        var original = new AllNumericTypesClass
        {
            ByteValue = 255,
            SByteValue = -128,
            ShortValue = -32768,
            UShortValue = 65535,
            UIntValue = 4294967295,
            ULongValue = 18446744073709551615,
            CharValue = 'Z'
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.AllNumericTypesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllNumericTypesClass);

        Assert.NotNull(result);
        Assert.Equal(original.ByteValue, result.ByteValue);
        Assert.Equal(original.SByteValue, result.SByteValue);
        Assert.Equal(original.ShortValue, result.ShortValue);
        Assert.Equal(original.UShortValue, result.UShortValue);
        Assert.Equal(original.UIntValue, result.UIntValue);
        Assert.Equal(original.ULongValue, result.ULongValue);
        Assert.Equal(original.CharValue, result.CharValue);
    }

    [Fact]
    public void RoundTripAllPrimitives()
    {
        var original = new AllPrimitivesClass
        {
            IntValue = int.MaxValue,
            LongValue = long.MaxValue,
            FloatValue = 3.14159f,
            DoubleValue = 2.71828,
            DecimalValue = 123.456m,
            BoolValue = true,
            StringValue = "AllPrimitives"
        };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.AllPrimitivesClass);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.AllPrimitivesClass);

        Assert.NotNull(result);
        Assert.Equal(original.IntValue, result.IntValue);
        Assert.Equal(original.LongValue, result.LongValue);
        Assert.Equal(original.BoolValue, result.BoolValue);
        Assert.Equal(original.StringValue, result.StringValue);
    }
}
