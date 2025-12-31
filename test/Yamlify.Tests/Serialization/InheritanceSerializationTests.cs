using Yamlify.Serialization;

namespace Yamlify.Tests.Serialization;

/// <summary>
/// Interface for polymorphism testing.
/// </summary>
public interface IAnimal
{
    string Name { get; }
    string Sound { get; }
}

/// <summary>
/// Concrete implementation of IAnimal.
/// </summary>
public class Dog : IAnimal
{
    public string Name { get; set; } = "";
    public string Sound => "Woof";
    public string Breed { get; set; } = "";
}

/// <summary>
/// Another concrete implementation of IAnimal.
/// </summary>
public class Cat : IAnimal
{
    public string Name { get; set; } = "";
    public string Sound => "Meow";
    public bool IsIndoor { get; set; }
}

/// <summary>
/// Abstract base class for inheritance testing.
/// </summary>
public abstract class Vehicle
{
    public string? Brand { get; set; }
    public int Year { get; set; }
    public abstract string Type { get; }
}

/// <summary>
/// Concrete class inheriting from Vehicle.
/// </summary>
public class Car : Vehicle
{
    public override string Type => "Car";
    public int Doors { get; set; }
}

/// <summary>
/// Concrete class inheriting from Vehicle.
/// </summary>
public class Motorcycle : Vehicle
{
    public override string Type => "Motorcycle";
    public bool HasSidecar { get; set; }
}

/// <summary>
/// Tests for serializing and deserializing classes with inheritance.
/// </summary>
public class InheritanceSerializationTests
{
    [Fact]
    public void SerializeConcreteClass()
    {
        var car = new Car { Brand = "Toyota", Year = 2023, Doors = 4 };

        var yaml = YamlSerializer.Serialize(car, TestSerializerContext.Default.Car);

        Assert.Contains("brand:", yaml);
        Assert.Contains("Toyota", yaml);
        Assert.Contains("year:", yaml);
        Assert.Contains("2023", yaml);
        Assert.Contains("doors:", yaml);
        Assert.Contains("4", yaml);
    }

    [Fact]
    public void DeserializeConcreteClass()
    {
        var yaml = """
            brand: Honda
            year: 2022
            doors: 2
            """;

        var car = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Car);

        Assert.NotNull(car);
        Assert.Equal("Honda", car.Brand);
        Assert.Equal(2022, car.Year);
        Assert.Equal(2, car.Doors);
    }

    [Fact]
    public void SerializeDifferentDerivedTypes()
    {
        var car = new Car { Brand = "Ford", Year = 2021, Doors = 4 };
        var motorcycle = new Motorcycle { Brand = "Harley", Year = 2020, HasSidecar = true };

        var carYaml = YamlSerializer.Serialize(car, TestSerializerContext.Default.Car);
        var motorcycleYaml = YamlSerializer.Serialize(motorcycle, TestSerializerContext.Default.Motorcycle);

        Assert.Contains("doors:", carYaml);
        Assert.Contains("has-sidecar:", motorcycleYaml);
        Assert.Contains("true", motorcycleYaml);
    }

    [Fact]
    public void SerializeInterfaceImplementation()
    {
        var dog = new Dog { Name = "Buddy", Breed = "Golden Retriever" };
        var cat = new Cat { Name = "Whiskers", IsIndoor = true };

        var dogYaml = YamlSerializer.Serialize(dog, TestSerializerContext.Default.Dog);
        var catYaml = YamlSerializer.Serialize(cat, TestSerializerContext.Default.Cat);

        Assert.Contains("name:", dogYaml);
        Assert.Contains("Buddy", dogYaml);
        Assert.Contains("breed:", dogYaml);
        Assert.Contains("is-indoor:", catYaml);
        Assert.Contains("true", catYaml);
    }

    [Fact]
    public void DeserializeDog()
    {
        var yaml = """
            name: Rex
            breed: German Shepherd
            """;

        var dog = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Dog);

        Assert.NotNull(dog);
        Assert.Equal("Rex", dog.Name);
        Assert.Equal("German Shepherd", dog.Breed);
        Assert.Equal("Woof", dog.Sound);
    }

    [Fact]
    public void DeserializeCat()
    {
        var yaml = """
            name: Fluffy
            is-indoor: true
            """;

        var cat = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Cat);

        Assert.NotNull(cat);
        Assert.Equal("Fluffy", cat.Name);
        Assert.True(cat.IsIndoor);
        Assert.Equal("Meow", cat.Sound);
    }

    [Fact]
    public void DeserializeMotorcycle()
    {
        var yaml = """
            brand: Ducati
            year: 2023
            has-sidecar: false
            """;

        var motorcycle = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Motorcycle);

        Assert.NotNull(motorcycle);
        Assert.Equal("Ducati", motorcycle.Brand);
        Assert.Equal(2023, motorcycle.Year);
        Assert.False(motorcycle.HasSidecar);
        Assert.Equal("Motorcycle", motorcycle.Type);
    }

    [Fact]
    public void RoundTripCar()
    {
        var original = new Car { Brand = "BMW", Year = 2024, Doors = 2 };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.Car);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Car);

        Assert.NotNull(result);
        Assert.Equal(original.Brand, result.Brand);
        Assert.Equal(original.Year, result.Year);
        Assert.Equal(original.Doors, result.Doors);
        Assert.Equal("Car", result.Type);
    }

    [Fact]
    public void RoundTripMotorcycle()
    {
        var original = new Motorcycle { Brand = "Yamaha", Year = 2022, HasSidecar = true };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.Motorcycle);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Motorcycle);

        Assert.NotNull(result);
        Assert.Equal(original.Brand, result.Brand);
        Assert.Equal(original.Year, result.Year);
        Assert.Equal(original.HasSidecar, result.HasSidecar);
    }

    [Fact]
    public void RoundTripDog()
    {
        var original = new Dog { Name = "Max", Breed = "Labrador" };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.Dog);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Dog);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Breed, result.Breed);
    }

    [Fact]
    public void RoundTripCat()
    {
        var original = new Cat { Name = "Mittens", IsIndoor = false };

        var yaml = YamlSerializer.Serialize(original, TestSerializerContext.Default.Cat);
        var result = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Cat);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.IsIndoor, result.IsIndoor);
    }

    [Fact]
    public void SerializeInheritedPropertiesAreIncluded()
    {
        var car = new Car { Brand = "Mercedes", Year = 2025, Doors = 4 };

        var yaml = YamlSerializer.Serialize(car, TestSerializerContext.Default.Car);

        // Inherited properties from Vehicle
        Assert.Contains("brand:", yaml);
        Assert.Contains("Mercedes", yaml);
        Assert.Contains("year:", yaml);
        Assert.Contains("2025", yaml);
        // Own property
        Assert.Contains("doors:", yaml);
        Assert.Contains("4", yaml);
    }

    [Fact]
    public void DeserializeWithMissingProperties()
    {
        var yaml = """
            name: Spot
            """;

        var dog = YamlSerializer.Deserialize(yaml, TestSerializerContext.Default.Dog);

        Assert.NotNull(dog);
        Assert.Equal("Spot", dog.Name);
        Assert.Equal("", dog.Breed);
    }
}
