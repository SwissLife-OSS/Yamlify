using Yamlify.Core;
using Yamlify.Schema;

namespace Yamlify.Serialization.Converters;

/// <summary>
/// Converter for string values.
/// </summary>
public sealed class StringConverter : YamlConverter<string>
{
    /// <inheritdoc/>
    public override string? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }
        
        var value = reader.GetString();
        reader.Read();
        return value;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, string value, YamlSerializerOptions options)
    {
        writer.WriteString(value);
    }
}

/// <summary>
/// Converter for boolean values.
/// </summary>
public sealed class BooleanConverter : YamlConverter<bool>
{
    /// <inheritdoc/>
    public override bool Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetBoolean(out var value) ? value : false;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, bool value, YamlSerializerOptions options)
    {
        writer.WriteBoolean(value);
    }
}

/// <summary>
/// Converter for 32-bit integer values.
/// </summary>
public sealed class Int32Converter : YamlConverter<int>
{
    /// <inheritdoc/>
    public override int Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetInt32(out var value) ? value : 0;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, int value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}

/// <summary>
/// Converter for 64-bit integer values.
/// </summary>
public sealed class Int64Converter : YamlConverter<long>
{
    /// <inheritdoc/>
    public override long Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetInt64(out var value) ? value : 0L;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, long value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}

/// <summary>
/// Converter for double-precision floating-point values.
/// </summary>
public sealed class DoubleConverter : YamlConverter<double>
{
    /// <inheritdoc/>
    public override double Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetDouble(out var value) ? value : 0.0;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, double value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}

/// <summary>
/// Converter for single-precision floating-point values.
/// </summary>
public sealed class SingleConverter : YamlConverter<float>
{
    /// <inheritdoc/>
    public override float Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var result = reader.TryGetDouble(out var value) ? (float)value : 0f;
        reader.Read();
        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, float value, YamlSerializerOptions options)
    {
        writer.WriteNumber(value);
    }
}

/// <summary>
/// Converter for decimal values.
/// </summary>
public sealed class DecimalConverter : YamlConverter<decimal>
{
    /// <inheritdoc/>
    public override decimal Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return decimal.TryParse(str, out var value) ? value : 0m;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, decimal value, YamlSerializerOptions options)
    {
        writer.WriteNumber((double)value);
    }
}

/// <summary>
/// Converter for DateTime values.
/// </summary>
public sealed class DateTimeConverter : YamlConverter<DateTime>
{
    /// <inheritdoc/>
    public override DateTime Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateTime.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateTime value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}

/// <summary>
/// Converter for DateTimeOffset values.
/// </summary>
public sealed class DateTimeOffsetConverter : YamlConverter<DateTimeOffset>
{
    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateTimeOffset.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateTimeOffset value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}

/// <summary>
/// Converter for DateOnly values.
/// </summary>
public sealed class DateOnlyConverter : YamlConverter<DateOnly>
{
    /// <inheritdoc/>
    public override DateOnly Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return DateOnly.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, DateOnly value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}

/// <summary>
/// Converter for TimeOnly values.
/// </summary>
public sealed class TimeOnlyConverter : YamlConverter<TimeOnly>
{
    /// <inheritdoc/>
    public override TimeOnly Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return TimeOnly.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, TimeOnly value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("O"));
    }
}

/// <summary>
/// Converter for TimeSpan values.
/// </summary>
public sealed class TimeSpanConverter : YamlConverter<TimeSpan>
{
    /// <inheritdoc/>
    public override TimeSpan Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return TimeSpan.TryParse(str, out var value) ? value : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, TimeSpan value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString("c"));
    }
}

/// <summary>
/// Converter for Guid values.
/// </summary>
public sealed class GuidConverter : YamlConverter<Guid>
{
    /// <inheritdoc/>
    public override Guid Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return Guid.TryParse(str, out var value) ? value : Guid.Empty;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Guid value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}

/// <summary>
/// Converter for Uri values.
/// </summary>
public sealed class UriConverter : YamlConverter<Uri>
{
    /// <inheritdoc/>
    public override Uri? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return str != null && Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Uri value, YamlSerializerOptions options)
    {
        writer.WriteString(value.OriginalString);
    }
}

/// <summary>
/// Converter for Version values.
/// </summary>
public sealed class VersionConverter : YamlConverter<Version>
{
    /// <inheritdoc/>
    public override Version? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        return str != null && Version.TryParse(str, out var value) ? value : null;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, Version value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}

/// <summary>
/// Converter for byte array values (Base64 encoded).
/// </summary>
public sealed class ByteArrayConverter : YamlConverter<byte[]>
{
    /// <inheritdoc/>
    public override byte[]? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }
        
        var str = reader.GetString();
        reader.Read();
        
        try
        {
            return str != null ? Convert.FromBase64String(str) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, byte[] value, YamlSerializerOptions options)
    {
        writer.WriteString(Convert.ToBase64String(value));
    }
}

/// <summary>
/// Converter for nullable value types.
/// </summary>
/// <typeparam name="T">The underlying value type.</typeparam>
public sealed class NullableConverter<T> : YamlConverter<T?> where T : struct
{
    private readonly YamlConverter<T> _underlyingConverter;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="underlyingConverter">The converter for the underlying type.</param>
    public NullableConverter(YamlConverter<T> underlyingConverter)
    {
        _underlyingConverter = underlyingConverter;
    }

    /// <inheritdoc/>
    public override T? Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        if (reader.IsNull())
        {
            reader.Read();
            return null;
        }

        return _underlyingConverter.Read(ref reader, options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, T? value, YamlSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNull();
            return;
        }

        _underlyingConverter.Write(writer, value.Value, options);
    }
}

/// <summary>
/// Converter for enum types.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public sealed class EnumConverter<T> : YamlConverter<T> where T : struct, Enum
{
    /// <inheritdoc/>
    public override T Read(ref Utf8YamlReader reader, YamlSerializerOptions options)
    {
        var str = reader.GetString();
        reader.Read();
        
        if (Enum.TryParse<T>(str, true, out var result))
        {
            return result;
        }
        
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        writer.WriteString(value.ToString());
    }
}
