namespace Yamlify;

/// <summary>
/// UTF-8 formatting utilities.
/// </summary>
internal static class Utf8Formatter
{
    public static bool TryFormat(int value, Span<byte> destination, out int bytesWritten)
    {
        return System.Buffers.Text.Utf8Formatter.TryFormat(value, destination, out bytesWritten);
    }

    public static bool TryFormat(long value, Span<byte> destination, out int bytesWritten)
    {
        return System.Buffers.Text.Utf8Formatter.TryFormat(value, destination, out bytesWritten);
    }

    public static bool TryFormat(double value, Span<byte> destination, out int bytesWritten)
    {
        return System.Buffers.Text.Utf8Formatter.TryFormat(value, destination, out bytesWritten);
    }
}
