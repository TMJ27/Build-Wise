using System.Globalization;

namespace BuildWise.Data;

public static class GeneratedNumberFormat
{
    public static string Format(string prefix, DateTime createdAt, int sequence)
    {
        return $"{prefix}-{createdAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{sequence.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}
