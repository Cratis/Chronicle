// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.Json;

/// <summary>
/// Provides JSON conversion capabilities for entity properties.
/// </summary>
public static class JsonConversion
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = false,
        Converters =
        {
            new ConceptAsJsonConverterFactory()
        }
    };

    /// <summary>
    /// Applies JSON conversion to all properties marked with the <see cref="JsonAttribute"/> in the specified <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder to apply the JSON conversion to.</param>
    /// <param name="providerName">The name of the database provider (e.g., "Npgsql", "SqlServer", "Sqlite").</param>
    public static void ApplyJsonConversion(this ModelBuilder modelBuilder, string? providerName = "")
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(JsonAttribute), inherit: true))
                .ToList();

            foreach (var property in properties)
            {
                var propertyBuilder = modelBuilder.Entity(entityType.Name).Property(property.Name);
                var converterType = typeof(JsonValueConverter<>).MakeGenericType(property.PropertyType);
                var comparerType = typeof(JsonValueComparer<>).MakeGenericType(property.PropertyType);
                var converter = Activator.CreateInstance(converterType) as ValueConverter;
                var comparer = Activator.CreateInstance(comparerType) as ValueComparer;

                propertyBuilder.HasConversion(converter);
                propertyBuilder.Metadata.SetValueConverter(converter);
                propertyBuilder.Metadata.SetValueComparer(comparer);

                if (!string.IsNullOrEmpty(providerName))
                {
                    if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                        propertyBuilder.HasColumnType("jsonb");
                    else if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                        propertyBuilder.HasColumnType("json");
                    else if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                        propertyBuilder.HasColumnType("TEXT");
                }
            }
        }
    }

    sealed class JsonValueConverter<T>() : ValueConverter<T?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, _jsonSerializerOptions),
            v => v == null ? default : JsonSerializer.Deserialize<T>(v, _jsonSerializerOptions))
        where T : class;

    sealed class JsonValueComparer<T>() : ValueComparer<T?>(
            (a, b) => JsonEquals(a, b, _jsonSerializerOptions),
            v => v == null ? 0 : JsonSerializer.Serialize(v, _jsonSerializerOptions).GetHashCode(),
            v => v == null ? default : JsonSerializer.Deserialize<T>(
                        JsonSerializer.Serialize(v, _jsonSerializerOptions), _jsonSerializerOptions))
            where T : class
    {
        static bool JsonEquals(T? a, T? b, JsonSerializerOptions opt)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return JsonSerializer.Serialize(a, opt) == JsonSerializer.Serialize(b, opt);
        }
    }
}
