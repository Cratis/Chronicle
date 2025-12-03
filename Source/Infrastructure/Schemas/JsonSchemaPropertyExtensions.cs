// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="IJsonSchemaProperty"/>.
/// </summary>
public static class JsonSchemaPropertyExtensions
{
    /// <summary>
    /// Check if the property is nullable.
    /// </summary>
    /// <param name="schemaProperty"><see cref="IJsonSchemaProperty"/> to check.</param>
    /// <returns>True if nullable, false if not.</returns>
    public static bool IsNullable(this IJsonSchemaProperty schemaProperty) => schemaProperty.Format?.EndsWith('?') ?? false;

    /// <summary>
    /// Get the target type for a <see cref="IJsonSchemaProperty"/>.
    /// </summary>
    /// <param name="schemaProperty"><see cref="IJsonSchemaProperty"/> to get for.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving types.</param>
    /// <returns>Target <see cref="Type"/> for the property or null if not found.</returns>
    public static Type? GetTargetTypeForJsonSchemaProperty(this IJsonSchemaProperty schemaProperty, ITypeFormats typeFormats)
    {
        if (schemaProperty.Format is null)
        {
            return null;
        }

        var format = schemaProperty.Format;
        var isNullable = false;

        // Handle nullable format marker
        if (format.EndsWith('?'))
        {
            format = format[..^1];
            isNullable = true;
        }

        if (!typeFormats.IsKnown(format))
        {
            return null;
        }

        var type = typeFormats.GetTypeForFormat(format);

        // Handle nullable types
        if (isNullable && type.IsValueType)
        {
            type = typeof(Nullable<>).MakeGenericType(type);
        }

        return type;
    }
}