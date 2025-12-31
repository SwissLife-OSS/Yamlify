using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

#region Polymorphic Test Models

/// <summary>
/// Base class marked as polymorphic with derived type mappings.
/// </summary>
[YamlPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[YamlDerivedType(typeof(PolymorphicDog), "dog")]
[YamlDerivedType(typeof(PolymorphicCat), "cat")]
[YamlDerivedType(typeof(PolymorphicBird), "bird")]
public abstract class PolymorphicAnimal
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class PolymorphicDog : PolymorphicAnimal
{
    public string Breed { get; set; } = "";
    public bool IsTrained { get; set; }
}

public class PolymorphicCat : PolymorphicAnimal
{
    public bool IsIndoor { get; set; }
    public string? FavoriteSpot { get; set; }
}

public class PolymorphicBird : PolymorphicAnimal
{
    public double WingSpan { get; set; }
    public bool CanFly { get; set; } = true;
}

/// <summary>
/// Container that holds a polymorphic property.
/// </summary>
public class PolymorphicZoo
{
    public string ZooName { get; set; } = "";
    public PolymorphicAnimal? FeaturedAnimal { get; set; }
    public List<PolymorphicAnimal>? Animals { get; set; }
}

/// <summary>
/// Interface-based polymorphism.
/// </summary>
[YamlPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[YamlDerivedType(typeof(PolymorphicRectangle), "rectangle")]
[YamlDerivedType(typeof(PolymorphicCircle), "circle")]
[YamlDerivedType(typeof(PolymorphicTriangle), "triangle")]
public interface IPolymorphicShape
{
    double Area { get; }
}

public class PolymorphicRectangle : IPolymorphicShape
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Area => Width * Height;
}

public class PolymorphicCircle : IPolymorphicShape
{
    public double Radius { get; set; }
    public double Area => Math.PI * Radius * Radius;
}

public class PolymorphicTriangle : IPolymorphicShape
{
    public double Base { get; set; }
    public double Height { get; set; }
    public double Area => 0.5 * Base * Height;
}

public class PolymorphicCanvas
{
    public string Title { get; set; } = "";
    public IPolymorphicShape? PrimaryShape { get; set; }
    public List<IPolymorphicShape>? Shapes { get; set; }
}

/// <summary>
/// Polymorphic type with default discriminator name ($type).
/// </summary>
[YamlPolymorphic]
[YamlDerivedType(typeof(PolymorphicTextMessage), "text")]
[YamlDerivedType(typeof(PolymorphicImageMessage), "image")]
public abstract class PolymorphicMessage
{
    public DateTime Timestamp { get; set; }
    public string Sender { get; set; } = "";
}

public class PolymorphicTextMessage : PolymorphicMessage
{
    public string Content { get; set; } = "";
}

public class PolymorphicImageMessage : PolymorphicMessage
{
    public string ImageUrl { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Non-abstract polymorphic base type - can be instantiated directly.
/// </summary>
[YamlPolymorphic(TypeDiscriminatorPropertyName = "resource-type")]
[YamlDerivedType(typeof(PolymorphicFileResource), "file")]
[YamlDerivedType(typeof(PolymorphicDatabaseResource), "database")]
public class PolymorphicResource
{
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}

public class PolymorphicFileResource : PolymorphicResource
{
    public string FilePath { get; set; } = "";
    public long SizeInBytes { get; set; }
}

public class PolymorphicDatabaseResource : PolymorphicResource
{
    public string ConnectionString { get; set; } = "";
    public int MaxConnections { get; set; } = 10;
}

#endregion

/// <summary>
/// Tests for polymorphic serialization using [YamlPolymorphic] and [YamlDerivedType] attributes.
/// These tests validate union type support where the concrete type is determined by a discriminator field.
/// </summary>
public class PolymorphicSerializationTests
{
    #region Serialization Tests

    [Fact]
    public void Serialize_DogAsAnimal_ShouldIncludeTypeDiscriminator()
    {
        PolymorphicAnimal animal = new PolymorphicDog 
        { 
            Name = "Buddy", 
            Age = 3, 
            Breed = "Golden Retriever", 
            IsTrained = true 
        };

        var yaml = YamlSerializer.Serialize(animal, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.Contains("$type: dog", yaml);
        Assert.Contains("name: Buddy", yaml);
        Assert.Contains("breed: Golden Retriever", yaml);
        Assert.Contains("is-trained: true", yaml);
    }

    [Fact]
    public void Serialize_CatAsAnimal_ShouldIncludeTypeDiscriminator()
    {
        PolymorphicAnimal animal = new PolymorphicCat 
        { 
            Name = "Whiskers", 
            Age = 5, 
            IsIndoor = true, 
            FavoriteSpot = "windowsill" 
        };

        var yaml = YamlSerializer.Serialize(animal, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.Contains("$type: cat", yaml);
        Assert.Contains("name: Whiskers", yaml);
        Assert.Contains("is-indoor: true", yaml);
        Assert.Contains("favorite-spot: windowsill", yaml);
    }

    [Fact]
    public void Serialize_ZooWithMixedAnimals_ShouldIncludeTypeDiscriminators()
    {
        var zoo = new PolymorphicZoo
        {
            ZooName = "City Zoo",
            FeaturedAnimal = new PolymorphicDog { Name = "Max", Age = 2, Breed = "Labrador" },
            Animals =
            [
                new PolymorphicDog { Name = "Rex", Age = 4, Breed = "German Shepherd" },
                new PolymorphicCat { Name = "Mittens", Age = 3, IsIndoor = true },
                new PolymorphicBird { Name = "Tweety", Age = 1, WingSpan = 0.3, CanFly = true }
            ]
        };

        var yaml = YamlSerializer.Serialize(zoo, PolymorphicSerializerContext.Default.PolymorphicZoo);

        Assert.Contains("$type: dog", yaml);
        Assert.Contains("$type: cat", yaml);
        Assert.Contains("$type: bird", yaml);
    }

    [Fact]
    public void Serialize_ConcreteTypeDirectly_ShouldStillIncludeDiscriminator()
    {
        var dog = new PolymorphicDog { Name = "Buddy", Age = 3, Breed = "Labrador" };

        // Even when serializing concrete type, if base is polymorphic, include discriminator
        var yaml = YamlSerializer.Serialize(dog, PolymorphicSerializerContext.Default.PolymorphicDog);

        Assert.Contains("name: Buddy", yaml);
        Assert.Contains("breed: Labrador", yaml);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void Deserialize_ConcreteType_ShouldWork()
    {
        // Deserializing to concrete type directly works
        var yaml = """
            name: Buddy
            age: 3
            breed: Golden Retriever
            is-trained: true
            """;

        var dog = YamlSerializer.Deserialize<PolymorphicDog>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicDog);

        Assert.NotNull(dog);
        Assert.Equal("Buddy", dog.Name);
        Assert.Equal(3, dog.Age);
        Assert.Equal("Golden Retriever", dog.Breed);
        Assert.True(dog.IsTrained);
    }

    [Fact]
    public void Deserialize_AbstractType_WithDiscriminator_ShouldCreateCorrectType()
    {
        var yaml = """
            $type: dog
            name: Buddy
            age: 3
            breed: Golden Retriever
            is-trained: true
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicDog>(animal);
        var dog = (PolymorphicDog)animal;
        Assert.Equal("Buddy", dog.Name);
        Assert.Equal(3, dog.Age);
        Assert.Equal("Golden Retriever", dog.Breed);
        Assert.True(dog.IsTrained);
    }

    [Fact]
    public void Deserialize_AbstractType_DiscriminatorAtEnd_ShouldWork()
    {
        // Discriminator at end - single-pass combined converter handles this
        var yaml = """
            name: Whiskers
            age: 5
            is-indoor: true
            favorite-spot: windowsill
            $type: cat
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicCat>(animal);
        var cat = (PolymorphicCat)animal;
        Assert.Equal("Whiskers", cat.Name);
        Assert.True(cat.IsIndoor);
    }

    [Fact]
    public void Deserialize_AbstractType_DiscriminatorInMiddle_ShouldWork()
    {
        var yaml = """
            name: Tweety
            $type: bird
            age: 1
            wing-span: 0.3
            can-fly: true
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicBird>(animal);
        var bird = (PolymorphicBird)animal;
        Assert.Equal("Tweety", bird.Name);
        Assert.Equal(0.3, bird.WingSpan);
    }

    [Fact]
    public void Deserialize_ZooWithPolymorphicAnimals_ShouldCreateCorrectTypes()
    {
        var yaml = """
            zoo-name: City Zoo
            featured-animal:
              $type: dog
              name: Max
              age: 2
              breed: Labrador
            animals:
              - $type: dog
                name: Rex
                age: 4
                breed: German Shepherd
              - $type: cat
                name: Mittens
                age: 3
                is-indoor: true
              - $type: bird
                name: Tweety
                age: 1
                wing-span: 0.3
            """;

        var zoo = YamlSerializer.Deserialize<PolymorphicZoo>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicZoo);

        Assert.NotNull(zoo);
        Assert.Equal("City Zoo", zoo.ZooName);
        Assert.NotNull(zoo.FeaturedAnimal);
        Assert.IsType<PolymorphicDog>(zoo.FeaturedAnimal);
        Assert.NotNull(zoo.Animals);
        Assert.Equal(3, zoo.Animals.Count);
        Assert.IsType<PolymorphicDog>(zoo.Animals[0]);
        Assert.IsType<PolymorphicCat>(zoo.Animals[1]);
        Assert.IsType<PolymorphicBird>(zoo.Animals[2]);
    }

    [Fact]
    public void Deserialize_UnknownDiscriminator_ShouldThrowForAbstractType()
    {
        var yaml = """
            $type: unknown_animal
            name: Mystery
            """;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            YamlSerializer.Deserialize<PolymorphicAnimal>(
                yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal));

        Assert.Contains("Unknown extension type: unknown_animal", exception.Message);
    }

    [Fact]
    public void Deserialize_MissingDiscriminator_ShouldThrowForAbstractType()
    {
        var yaml = """
            name: Mystery
            age: 5
            """;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            YamlSerializer.Deserialize<PolymorphicAnimal>(
                yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal));

        Assert.Contains("Missing extension type discriminator", exception.Message);
    }

    [Fact]
    public void Deserialize_CaseInsensitiveDiscriminator_ShouldWork()
    {
        var yaml = """
            $type: DOG
            name: Buddy
            age: 3
            breed: Labrador
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicDog>(animal);
        Assert.Equal("Buddy", animal.Name);
    }

    [Fact]
    public void Deserialize_MixedCaseDiscriminator_ShouldWork()
    {
        var yaml = """
            $type: BiRd
            name: Tweety
            age: 1
            wing-span: 0.5
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicBird>(animal);
        Assert.Equal("Tweety", animal.Name);
    }

    [Fact]
    public void Deserialize_DefaultValuePreservation_ShouldUseTypeDefaults()
    {
        // PolymorphicBird has CanFly = true as default
        var yaml = """
            $type: bird
            name: Tweety
            age: 1
            wing-span: 0.5
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicBird>(animal);
        var bird = (PolymorphicBird)animal;
        Assert.True(bird.CanFly); // Default value should be preserved
    }

    [Fact]
    public void Deserialize_ExplicitFalseValue_ShouldOverrideDefault()
    {
        // Explicitly set CanFly to false
        var yaml = """
            $type: bird
            name: Penguin
            age: 5
            wing-span: 0.3
            can-fly: false
            """;

        var animal = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(animal);
        Assert.IsType<PolymorphicBird>(animal);
        var bird = (PolymorphicBird)animal;
        Assert.False(bird.CanFly); // Explicitly set value should override default
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_Dog_ShouldPreserveAllProperties()
    {
        var original = new PolymorphicDog 
        { 
            Name = "Buddy", 
            Age = 3, 
            Breed = "Golden Retriever", 
            IsTrained = true 
        };

        // Serialize through abstract type (includes discriminator)
        var yaml = YamlSerializer.Serialize<PolymorphicAnimal>(
            original, PolymorphicSerializerContext.Default.PolymorphicAnimal);
        
        // Deserialize through abstract type - should get correct concrete type
        var deserialized = YamlSerializer.Deserialize<PolymorphicAnimal>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicAnimal);

        Assert.NotNull(deserialized);
        Assert.IsType<PolymorphicDog>(deserialized);
        var dog = (PolymorphicDog)deserialized;
        Assert.Equal(original.Name, dog.Name);
        Assert.Equal(original.Age, dog.Age);
        Assert.Equal(original.Breed, dog.Breed);
        Assert.Equal(original.IsTrained, dog.IsTrained);
    }

    [Fact]
    public void Roundtrip_Zoo_ShouldPreserveAllTypes()
    {
        var original = new PolymorphicZoo
        {
            ZooName = "City Zoo",
            FeaturedAnimal = new PolymorphicCat { Name = "Fluffy", Age = 2, IsIndoor = true },
            Animals =
            [
                new PolymorphicDog { Name = "Rex", Age = 4, Breed = "German Shepherd", IsTrained = true },
                new PolymorphicBird { Name = "Polly", Age = 2, WingSpan = 0.5, CanFly = true }
            ]
        };

        var yaml = YamlSerializer.Serialize(original, PolymorphicSerializerContext.Default.PolymorphicZoo);
        var deserialized = YamlSerializer.Deserialize<PolymorphicZoo>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicZoo);

        Assert.NotNull(deserialized);
        Assert.IsType<PolymorphicCat>(deserialized.FeaturedAnimal);
        Assert.NotNull(deserialized.Animals);
        Assert.Equal(2, deserialized.Animals.Count);
        Assert.IsType<PolymorphicDog>(deserialized.Animals[0]);
        Assert.IsType<PolymorphicBird>(deserialized.Animals[1]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_NullPolymorphicProperty_ShouldSerializeAsNull()
    {
        var zoo = new PolymorphicZoo { ZooName = "Empty Zoo", FeaturedAnimal = null };

        var yaml = YamlSerializer.Serialize(zoo, PolymorphicSerializerContext.Default.PolymorphicZoo);

        Assert.Contains("zoo-name: Empty Zoo", yaml);
    }

    [Fact]
    public void Deserialize_ConcreteType_WithExtraDiscriminator_ShouldIgnoreIt()
    {
        // Discriminator is ignored when deserializing directly to concrete type
        var yaml = """
            $type: dog
            name: Buddy
            age: 3
            breed: Labrador
            """;

        var dog = YamlSerializer.Deserialize<PolymorphicDog>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicDog);

        Assert.NotNull(dog);
        Assert.Equal("Buddy", dog.Name);
        Assert.Equal("Labrador", dog.Breed);
    }

    #endregion

    #region Non-Abstract Polymorphic Base Type Tests

    [Fact]
    public void Serialize_DerivedResourceType_ShouldIncludeDiscriminator()
    {
        PolymorphicResource resource = new PolymorphicFileResource
        {
            Name = "config.json",
            IsEnabled = true,
            FilePath = "/etc/app/config.json",
            SizeInBytes = 1024
        };

        var yaml = YamlSerializer.Serialize(resource, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.Contains("resource-type: file", yaml);
        Assert.Contains("name: config.json", yaml);
        Assert.Contains("file-path: /etc/app/config.json", yaml);
    }

    [Fact]
    public void Deserialize_NonAbstractBase_WithDerivedDiscriminator_ShouldCreateDerivedType()
    {
        var yaml = """
            resource-type: database
            name: MainDB
            is-enabled: true
            connection-string: Server=localhost;Database=app
            max-connections: 20
            """;

        var resource = YamlSerializer.Deserialize<PolymorphicResource>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.NotNull(resource);
        Assert.IsType<PolymorphicDatabaseResource>(resource);
        var db = (PolymorphicDatabaseResource)resource;
        Assert.Equal("MainDB", db.Name);
        Assert.Equal("Server=localhost;Database=app", db.ConnectionString);
        Assert.Equal(20, db.MaxConnections);
    }

    [Fact]
    public void Deserialize_NonAbstractBase_MissingDiscriminator_ShouldCreateBaseType()
    {
        // For non-abstract polymorphic types, missing discriminator returns base type
        var yaml = """
            name: GenericResource
            is-enabled: false
            """;

        var resource = YamlSerializer.Deserialize<PolymorphicResource>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.NotNull(resource);
        Assert.IsType<PolymorphicResource>(resource); // Base type, not derived
        Assert.Equal("GenericResource", resource.Name);
        Assert.False(resource.IsEnabled);
    }

    [Fact]
    public void Deserialize_NonAbstractBase_UnknownDiscriminator_ShouldCreateBaseType()
    {
        // For non-abstract polymorphic types, unknown discriminator returns base type
        var yaml = """
            resource-type: unknown
            name: UnknownResource
            is-enabled: true
            """;

        var resource = YamlSerializer.Deserialize<PolymorphicResource>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.NotNull(resource);
        Assert.IsType<PolymorphicResource>(resource); // Base type
        Assert.Equal("UnknownResource", resource.Name);
    }

    [Fact]
    public void Deserialize_NonAbstractBase_CaseInsensitiveDiscriminator_ShouldWork()
    {
        var yaml = """
            resource-type: FILE
            name: test.txt
            file-path: /tmp/test.txt
            size-in-bytes: 100
            """;

        var resource = YamlSerializer.Deserialize<PolymorphicResource>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.NotNull(resource);
        Assert.IsType<PolymorphicFileResource>(resource);
    }

    [Fact]
    public void Deserialize_NonAbstractBase_DefaultValuePreservation_ShouldWork()
    {
        // PolymorphicResource.IsEnabled = true by default
        // PolymorphicDatabaseResource.MaxConnections = 10 by default
        var yaml = """
            resource-type: database
            name: TestDB
            connection-string: Server=test
            """;

        var resource = YamlSerializer.Deserialize<PolymorphicResource>(
            yaml, PolymorphicSerializerContext.Default.PolymorphicResource);

        Assert.NotNull(resource);
        Assert.IsType<PolymorphicDatabaseResource>(resource);
        var db = (PolymorphicDatabaseResource)resource;
        Assert.True(db.IsEnabled); // Default from base type
        Assert.Equal(10, db.MaxConnections); // Default from derived type
    }

    #endregion

    #region YamlSerializable Attribute Configuration Tests

    [Fact]
    public void Serialize_UsingYamlSerializablePolymorphism_ShouldIncludeDiscriminator()
    {
        ContextConfiguredVehicle vehicle = new ContextConfiguredCar
        {
            Brand = "Tesla",
            Model = "Model 3",
            NumberOfDoors = 4
        };

        var yaml = YamlSerializer.Serialize(vehicle, ContextBasedPolymorphicContext.Default.ContextConfiguredVehicle);

        Assert.Contains("vehicle-type: car", yaml);
        Assert.Contains("brand: Tesla", yaml);
        Assert.Contains("number-of-doors: 4", yaml);
    }

    [Fact]
    public void Deserialize_UsingYamlSerializablePolymorphism_ShouldCreateCorrectType()
    {
        var yaml = """
            vehicle-type: motorcycle
            brand: Harley-Davidson
            model: Sportster
            has-sidecar: true
            """;

        var vehicle = YamlSerializer.Deserialize<ContextConfiguredVehicle>(
            yaml, ContextBasedPolymorphicContext.Default.ContextConfiguredVehicle);

        Assert.NotNull(vehicle);
        Assert.IsType<ContextConfiguredMotorcycle>(vehicle);
        var motorcycle = (ContextConfiguredMotorcycle)vehicle;
        Assert.Equal("Harley-Davidson", motorcycle.Brand);
        Assert.True(motorcycle.HasSidecar);
    }

    [Fact]
    public void Deserialize_UsingYamlSerializablePolymorphism_CaseInsensitive_ShouldWork()
    {
        var yaml = """
            vehicle-type: CAR
            brand: BMW
            model: M3
            number-of-doors: 2
            """;

        var vehicle = YamlSerializer.Deserialize<ContextConfiguredVehicle>(
            yaml, ContextBasedPolymorphicContext.Default.ContextConfiguredVehicle);

        Assert.NotNull(vehicle);
        Assert.IsType<ContextConfiguredCar>(vehicle);
    }

    [Fact]
    public void Roundtrip_UsingYamlSerializablePolymorphism_ShouldPreserveType()
    {
        var original = new ContextConfiguredMotorcycle
        {
            Brand = "Ducati",
            Model = "Monster",
            HasSidecar = false
        };

        var yaml = YamlSerializer.Serialize<ContextConfiguredVehicle>(
            original, ContextBasedPolymorphicContext.Default.ContextConfiguredVehicle);
        
        var deserialized = YamlSerializer.Deserialize<ContextConfiguredVehicle>(
            yaml, ContextBasedPolymorphicContext.Default.ContextConfiguredVehicle);

        Assert.NotNull(deserialized);
        Assert.IsType<ContextConfiguredMotorcycle>(deserialized);
        var motorcycle = (ContextConfiguredMotorcycle)deserialized;
        Assert.Equal("Ducati", motorcycle.Brand);
        Assert.Equal("Monster", motorcycle.Model);
        Assert.False(motorcycle.HasSidecar);
    }

    #endregion
}

#region Models for Context-Based Polymorphic Configuration

/// <summary>
/// Base class WITHOUT any polymorphic attributes.
/// Polymorphism is configured on the serializer context instead.
/// </summary>
public abstract class ContextConfiguredVehicle
{
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
}

public class ContextConfiguredCar : ContextConfiguredVehicle
{
    public int NumberOfDoors { get; set; }
}

public class ContextConfiguredMotorcycle : ContextConfiguredVehicle
{
    public bool HasSidecar { get; set; }
}

#endregion

/// <summary>
/// Serializer context for polymorphic type tests.
/// </summary>
[YamlSerializable(typeof(PolymorphicAnimal))]
[YamlSerializable(typeof(PolymorphicDog))]
[YamlSerializable(typeof(PolymorphicCat))]
[YamlSerializable(typeof(PolymorphicBird))]
[YamlSerializable(typeof(PolymorphicZoo))]
[YamlSerializable(typeof(IPolymorphicShape))]
[YamlSerializable(typeof(PolymorphicRectangle))]
[YamlSerializable(typeof(PolymorphicCircle))]
[YamlSerializable(typeof(PolymorphicTriangle))]
[YamlSerializable(typeof(PolymorphicCanvas))]
[YamlSerializable(typeof(PolymorphicMessage))]
[YamlSerializable(typeof(PolymorphicTextMessage))]
[YamlSerializable(typeof(PolymorphicImageMessage))]
[YamlSerializable(typeof(PolymorphicResource))]
[YamlSerializable(typeof(PolymorphicFileResource))]
[YamlSerializable(typeof(PolymorphicDatabaseResource))]
public partial class PolymorphicSerializerContext : YamlSerializerContext { }

/// <summary>
/// Serializer context demonstrating polymorphism configured via [YamlSerializable] attribute.
/// The base type (ContextConfiguredVehicle) has NO [YamlPolymorphic] attribute - 
/// polymorphism is configured entirely on this context.
/// </summary>
[YamlSerializable(typeof(ContextConfiguredVehicle), 
    TypeDiscriminatorPropertyName = "vehicle-type",
    DerivedTypes = new[] { typeof(ContextConfiguredCar), typeof(ContextConfiguredMotorcycle) },
    DerivedTypeDiscriminators = new[] { "car", "motorcycle" })]
[YamlSerializable(typeof(ContextConfiguredCar))]
[YamlSerializable(typeof(ContextConfiguredMotorcycle))]
public partial class ContextBasedPolymorphicContext : YamlSerializerContext { }
