// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    /// <summary>
    /// Get all actual properties from a schema, including any inherited properties from inherited schemas.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="JsonSchemaProperty"/>.</returns>
    public static IEnumerable<JsonSchemaProperty> GetFlattenedProperties(this JsonSchema schema) =>
        schema.FlattenedProperties;

    /// <summary>
    /// Checks if the schema has a key property.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>Whether there is a key property.</returns>
    public static bool HasKeyProperty(this JsonSchema schema) =>
        schema.Properties.ContainsKey("id") || schema.Properties.ContainsKey("Id");

    /// <summary>
    /// Gets the key property from the schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>The key <see cref="JsonSchemaProperty"/>.</returns>
    public static JsonSchemaProperty GetKeyProperty(this JsonSchema schema)
    {
        var idPropertyName = schema.Properties.ContainsKey("id") ? "id" : "Id";
        return schema.Properties[idPropertyName];
    }

    /// <summary>
    /// Gets the likely key property name from the schema based on property naming conventions (camel vs pascal) of existing properties.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>The likely key property name.</returns>
    public static string GetLikelyKeyPropertyName(this JsonSchema schema)
    {
        var properties = schema.GetFlattenedProperties().Select(_ => _.Name).ToList();
        if (properties.Count == 0)
        {
            return null!;
        }

        var camelCaseCount = properties.Count(name => char.IsLower(name[0]));
        var pascalCaseCount = properties.Count(name => char.IsUpper(name[0]));

        return camelCaseCount > pascalCaseCount ? "id" : "Id";
    }

    /// <summary>
    /// Get whether the schema describes an opaque value — an object whose members it deliberately does not declare.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True when the schema describes an opaque value, false when it declares the members of what it describes.</returns>
    /// <remarks>
    /// Not every JSON object in a document is a container of schema-declared properties. Chronicle emits object
    /// schemas that declare nothing on purpose, because the value is stored and materialized as one typed value
    /// rather than as a set of members: a geospatial leaf, whose GeoJSON <c>type</c>/<c>coordinates</c> pair belongs
    /// to its converter; a polymorphic base type, kept open so a derived payload and its type discriminator round-trip
    /// intact; and a dictionary, whose members are data rather than declarations. Anything walking a document against
    /// its schema has to stop at such a value instead of reading its members as properties the schema forgot.
    /// <para>
    /// A schema that declares nothing because its declaration could not be reached — an unresolved <c>$ref</c>, a
    /// composition that flattened to nothing — is not opaque, it is broken. Calling it opaque would silently skip a
    /// value the schema does mean to describe, so those shapes are excluded and left to fail where they are used.
    /// </para>
    /// </remarks>
    public static bool DescribesOpaqueValue(this JsonSchema schema)
    {
        var actual = schema.ActualTypeSchema;

        if (actual.GetFlattenedProperties().Any() ||
            actual.HasReference ||
            actual.AllOf.Count > 0 ||
            actual.AnyOf.Count > 0 ||
            actual.OneOf.Count > 0)
        {
            return false;
        }

        return actual.Type.HasFlag(JsonObjectType.Object);
    }

    /// <summary>
    /// Gets the schema for a property within the schema hierarchy based on a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The actual <see cref="JsonSchema"/>.</returns>
    public static JsonSchema GetSchemaForPropertyPath(this JsonSchema schema, PropertyPath propertyPath)
    {
        foreach (var segment in propertyPath.Segments)
        {
            var properties = schema!.GetFlattenedProperties();
            var schemaProperty = properties.SingleOrDefault(_ => _.Name.Equals(segment.Value, StringComparison.OrdinalIgnoreCase));
            if (schemaProperty is not null)
            {
                if (schemaProperty.IsArray)
                {
                    schema = schemaProperty.Item!.Reference ?? schemaProperty.Item;
                }
                else
                {
                    schema = schemaProperty.ActualTypeSchema;
                }
            }
        }

        return schema;
    }

    /// <summary>
    /// Gets the <see cref="JsonSchemaProperty"/> within the schema hierarchy based on a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The actual <see cref="JsonSchemaProperty"/>.</returns>
    public static JsonSchemaProperty? GetSchemaPropertyForPropertyPath(this JsonSchema schema, PropertyPath propertyPath)
    {
        var segments = propertyPath.Segments.ToArray();
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            var properties = schema!.GetFlattenedProperties();
            var segment = segments[segmentIndex];

            var schemaProperty = properties.SingleOrDefault(_ => _.Name.Equals(segment.Value, StringComparison.OrdinalIgnoreCase));
            if (schemaProperty is not null)
            {
                if (segmentIndex == segments.Length - 1)
                {
                    return schemaProperty;
                }

                if (schemaProperty.IsArray)
                {
                    schema = schemaProperty.Item!.Reference ?? schemaProperty.Item;
                }
                else
                {
                    schema = schemaProperty.ActualTypeSchema;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the type for a <see cref="PropertyPath"/> resolved through the <see cref="JsonSchema"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to find property from.</param>
    /// <param name="propertyPath"><see cref="PropertyPath"/> for the property.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The actual type or null if its not a known property path within the schema.</returns>
    public static Type? GetTargetTypeForPropertyPath(this JsonSchema schema, PropertyPath propertyPath, ITypeFormats typeFormats)
    {
        var schemaProperty = schema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (schemaProperty is not null)
        {
            return schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);
        }

        return null;
    }

    /// <summary>
    /// Gets the type for a <see cref="PropertyPath"/> resolved through the <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to find property from.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The actual type or null if its not a known property path within the schema.</returns>
    public static Type? GetTargetTypeForJsonSchemaProperty(this JsonSchemaProperty schemaProperty, ITypeFormats typeFormats)
    {
        if (!string.IsNullOrEmpty(schemaProperty.Format) && typeFormats.IsKnown(schemaProperty.Format))
        {
            return typeFormats.GetTypeForFormat(schemaProperty.Format);
        }

        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                    schemaProperty.Reference?.Type ??
                        (schemaProperty.HasOneOfSchemaReference ?
                            schemaProperty.OneOf[0].Reference?.Type ?? JsonObjectType.None :
                            JsonObjectType.None) :
                    schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        return type switch
        {
            JsonObjectType.String => typeof(string),
            JsonObjectType.Boolean => typeof(bool),
            JsonObjectType.Integer => typeof(int),
            JsonObjectType.Number => typeof(double),
            _ => null
        };
    }

    /// <summary>
    /// Get the default value for a <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to get default value for.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> holding known JSON schema type formats.</param>
    /// <returns>The default value.</returns>
    /// <remarks>
    /// The default is what an unset property materializes as when a document is round-tripped through its schema,
    /// so it must be a value the property itself allows. A property that declares its members - an enum - allows
    /// only those, and a type default outside that set is a value the read model's own registered schema forbids.
    /// Writing one leaves a reader with two bad choices: refuse it, and take a whole observable query down rather
    /// than one row, or round it off to something that reads like a deliberate answer.
    /// </remarks>
    public static object? GetDefaultValue(this JsonSchemaProperty schemaProperty, ITypeFormats typeFormats)
    {
        if (schemaProperty.IsNullable())
        {
            return null;
        }

        var type = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);

        if (type is not null && (type.IsPrimitive || !type.IsByRef) &&
                type != typeof(string) &&
                type != typeof(object))
        {
            try
            {
                var defaultValue = Activator.CreateInstance(type);
                return IsDeclaredMember(schemaProperty, defaultValue) ? defaultValue : null;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Get whether or not the property is nullable.
    /// </summary>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <returns>True if it is, false if not.</returns>
    /// <remarks>
    /// Nullability is expressed two ways and a property carries whichever fits it. A formatted type - a date, a
    /// decimal - marks it with a trailing <c>?</c> on the format. A type with no format - an enum, a boolean -
    /// has nowhere to put the marker and declares <c>"null"</c> in its type instead. Testing only the format
    /// suffix therefore read every nullable enum and every nullable flag as non-nullable, and the round trip
    /// materialized a type default for a property whose whole point was that it might not have one.
    /// </remarks>
    public static bool IsNullable(this JsonSchemaProperty schemaProperty) =>
        (schemaProperty.Format?.EndsWith('?') ?? false) ||
        schemaProperty.Type.HasFlag(JsonObjectType.Null);

    /// <summary>
    /// Determines whether two schemas are equal once nullability markers are ignored — a trailing <c>?</c>
    /// appended to a <c>format</c> value to signal a nullable type. The marker only refines how an unset value
    /// materializes (null rather than a type-default sentinel) and does not change the data shape, so a
    /// marker-only difference must not be treated as a breaking schema change — for example when comparing a
    /// stored event schema against a newly generated one after a Chronicle upgrade.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> to compare.</param>
    /// <param name="other">The <see cref="JsonSchema"/> to compare against.</param>
    /// <returns><see langword="true"/> when the schemas are equal ignoring nullability markers; otherwise <see langword="false"/>.</returns>
    public static bool EqualsIgnoringNullableFormatMarkers(this JsonSchema schema, JsonSchema other) =>
        WithoutNullableFormatMarkers(schema.ToJson()) == WithoutNullableFormatMarkers(other.ToJson());

    static string WithoutNullableFormatMarkers(string schemaJson)
    {
        var node = JsonNode.Parse(schemaJson);
        StripNullableFormatMarkers(node);
        return node?.ToJsonString() ?? schemaJson;
    }

    static void StripNullableFormatMarkers(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (jsonObject["format"] is JsonValue formatValue &&
                    formatValue.TryGetValue<string>(out var format) &&
                    format.EndsWith('?'))
                {
                    jsonObject["format"] = format[..^1];
                }

                foreach (var property in jsonObject.ToArray().Where(_ => _.Key != "format"))
                {
                    StripNullableFormatMarkers(property.Value);
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray.ToArray())
                {
                    StripNullableFormatMarkers(item);
                }

                break;
        }
    }

    static bool IsDeclaredMember(JsonSchemaProperty schemaProperty, object? value)
    {
        if (schemaProperty.Enumeration.Count == 0 || value is null)
        {
            return true;
        }

        var asLong = Convert.ToInt64(value, CultureInfo.InvariantCulture);
        return schemaProperty.Enumeration.Any(member => member is not null && Convert.ToInt64(member, CultureInfo.InvariantCulture) == asLong);
    }
}
