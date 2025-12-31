using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Yamlify.Serialization;

namespace Yamlify.Benchmarks;

#region Test Models

public class SimplePerson
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}

public class ComplexOrder
{
    public string OrderId { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public Customer Customer { get; set; } = new();
    public List<OrderItem> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public bool IsShipped { get; set; }
    public string? Notes { get; set; }
}

public class Customer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Address Address { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string PostalCode { get; set; } = "";
}

public class OrderItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

[YamlSerializable(typeof(SimplePerson))]
[YamlSerializable(typeof(ComplexOrder))]
[YamlSerializable(typeof(Customer))]
[YamlSerializable(typeof(Address))]
[YamlSerializable(typeof(OrderItem))]
public partial class BenchmarkSerializerContext : YamlSerializerContext { }

#endregion

/// <summary>
/// Baseline performance tests to detect regressions.
/// These tests establish performance baselines and fail if operations take too long.
/// </summary>
public class BaselinePerformanceTests(ITestOutputHelper output)
{
    private const int WarmupIterations = 100;
    private const int MeasureIterations = 10000;

    // Baseline thresholds (in milliseconds for 10000 iterations)
    // These should be updated if intentional performance changes are made
    private const int SimpleSerializeMaxMs = 500;
    private const int SimpleDeserializeMaxMs = 500;
    private const int ComplexSerializeMaxMs = 2000;
    private const int ComplexDeserializeMaxMs = 2000;

    private static SimplePerson CreateSimplePerson() => new()
    {
        Name = "John Doe",
        Age = 30,
        Email = "john.doe@example.com"
    };

    private static ComplexOrder CreateComplexOrder() => new()
    {
        OrderId = "ORD-2024-001",
        OrderDate = new DateTime(2024, 1, 15, 10, 30, 0),
        Customer = new Customer
        {
            Id = "CUST-001",
            Name = "Jane Smith",
            Address = new Address
            {
                Street = "123 Main Street",
                City = "New York",
                Country = "USA",
                PostalCode = "10001"
            }
        },
        Items =
        [
            new OrderItem { ProductId = "PROD-001", ProductName = "Widget A", Quantity = 5, UnitPrice = 19.99m },
            new OrderItem { ProductId = "PROD-002", ProductName = "Gadget B", Quantity = 2, UnitPrice = 49.99m },
            new OrderItem { ProductId = "PROD-003", ProductName = "Thingamajig C", Quantity = 10, UnitPrice = 9.99m }
        ],
        TotalAmount = 299.83m,
        IsShipped = true,
        Notes = "Please deliver before noon"
    };

    [Fact]
    public void Baseline_SimpleSerialize()
    {
        var person = CreateSimplePerson();

        // Warm up
        for (int i = 0; i < WarmupIterations; i++)
        {
            YamlSerializer.Serialize(person, BenchmarkSerializerContext.Default.SimplePerson);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < MeasureIterations; i++)
        {
            YamlSerializer.Serialize(person, BenchmarkSerializerContext.Default.SimplePerson);
        }
        sw.Stop();

        var avgNs = sw.Elapsed.TotalNanoseconds / MeasureIterations;
        output.WriteLine($"Simple Serialize: {avgNs:F0} ns/op ({sw.ElapsedMilliseconds} ms for {MeasureIterations} iterations)");

        Assert.True(sw.ElapsedMilliseconds < SimpleSerializeMaxMs,
            $"Simple serialize took {sw.ElapsedMilliseconds}ms, expected < {SimpleSerializeMaxMs}ms. Performance regression detected!");
    }

    [Fact]
    public void Baseline_SimpleDeserialize()
    {
        var person = CreateSimplePerson();
        var yaml = YamlSerializer.Serialize(person, BenchmarkSerializerContext.Default.SimplePerson);

        // Warm up
        for (int i = 0; i < WarmupIterations; i++)
        {
            YamlSerializer.Deserialize(yaml, BenchmarkSerializerContext.Default.SimplePerson);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < MeasureIterations; i++)
        {
            YamlSerializer.Deserialize(yaml, BenchmarkSerializerContext.Default.SimplePerson);
        }
        sw.Stop();

        var avgNs = sw.Elapsed.TotalNanoseconds / MeasureIterations;
        output.WriteLine($"Simple Deserialize: {avgNs:F0} ns/op ({sw.ElapsedMilliseconds} ms for {MeasureIterations} iterations)");

        Assert.True(sw.ElapsedMilliseconds < SimpleDeserializeMaxMs,
            $"Simple deserialize took {sw.ElapsedMilliseconds}ms, expected < {SimpleDeserializeMaxMs}ms. Performance regression detected!");
    }

    [Fact]
    public void Baseline_ComplexSerialize()
    {
        var order = CreateComplexOrder();

        // Warm up
        for (int i = 0; i < WarmupIterations; i++)
        {
            YamlSerializer.Serialize(order, BenchmarkSerializerContext.Default.ComplexOrder);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < MeasureIterations; i++)
        {
            YamlSerializer.Serialize(order, BenchmarkSerializerContext.Default.ComplexOrder);
        }
        sw.Stop();

        var avgNs = sw.Elapsed.TotalNanoseconds / MeasureIterations;
        output.WriteLine($"Complex Serialize: {avgNs:F0} ns/op ({sw.ElapsedMilliseconds} ms for {MeasureIterations} iterations)");

        Assert.True(sw.ElapsedMilliseconds < ComplexSerializeMaxMs,
            $"Complex serialize took {sw.ElapsedMilliseconds}ms, expected < {ComplexSerializeMaxMs}ms. Performance regression detected!");
    }

    [Fact]
    public void Baseline_ComplexDeserialize()
    {
        var order = CreateComplexOrder();
        var yaml = YamlSerializer.Serialize(order, BenchmarkSerializerContext.Default.ComplexOrder);

        // Warm up
        for (int i = 0; i < WarmupIterations; i++)
        {
            YamlSerializer.Deserialize(yaml, BenchmarkSerializerContext.Default.ComplexOrder);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < MeasureIterations; i++)
        {
            YamlSerializer.Deserialize(yaml, BenchmarkSerializerContext.Default.ComplexOrder);
        }
        sw.Stop();

        var avgNs = sw.Elapsed.TotalNanoseconds / MeasureIterations;
        output.WriteLine($"Complex Deserialize: {avgNs:F0} ns/op ({sw.ElapsedMilliseconds} ms for {MeasureIterations} iterations)");

        Assert.True(sw.ElapsedMilliseconds < ComplexDeserializeMaxMs,
            $"Complex deserialize took {sw.ElapsedMilliseconds}ms, expected < {ComplexDeserializeMaxMs}ms. Performance regression detected!");
    }
}
