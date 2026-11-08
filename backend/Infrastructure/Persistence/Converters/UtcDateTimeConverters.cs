using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChatApp.Infrastructure.Persistence.Converters;

/// <summary>
/// SQL Server returns datetime2 values with DateTimeKind.Unspecified, so they serialize to JSON
/// without a "Z" and clients read them as local time. Everything we store is UTC, so mark it as such.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
