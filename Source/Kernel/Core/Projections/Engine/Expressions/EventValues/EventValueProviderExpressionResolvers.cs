// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Reflection;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Represents an implementation of <see cref="IReadModelPropertyExpressionResolvers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ITypeFormats"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for finding target types.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public partial class EventValueProviderExpressionResolvers(ITypeFormats typeFormats, ILogger<EventValueProviderExpressionResolvers> logger) : IEventValueProviderExpressionResolvers
{
    readonly IEventValueProviderExpressionResolver[] _resolvers =
    [
        new EventSourceIdExpressionResolver(),
        new NullExpressionResolver(),
        new EventContextPropertyExpressionResolver(),
        new CausedByExpressionResolver(),
        new LiteralExpressionResolver(),
        new EventContentExpressionResolver(),
        new ValueExpressionResolver()
    ];

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _resolvers.Any(_ => _.CanResolve(expression));

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(JsonSchemaProperty targetSchemaProperty, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
        ThrowIfUnsupportedEventValueExpression(targetSchemaProperty, expression, resolver);

        return (e) => Convert(targetSchemaProperty, resolver!.Resolve(expression)(e));
    }

    static bool TryGetWrappedScalarValue(IDictionary<string, object> input, JsonSchemaProperty schemaProperty, ITypeFormats typeFormats, out object? value)
    {
        value = null;
        return input.Count == 1 &&
            input.TryGetValue("value", out value) &&
            schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats) is not null;
    }

    /// <summary>
    /// Dictionaries are values to preserve, not sequences of key/value pairs to project as arrays.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="inputType">The type of the input value.</param>
    /// <returns><see langword="true"/> if the input is a dictionary; otherwise <see langword="false"/>.</returns>
    static bool IsDictionary(object input, Type inputType) =>
        input is IDictionary ||
        inputType.IsDictionary() ||
        inputType.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    static JsonSchemaProperty GetItemSchemaProperty(JsonSchemaProperty schemaProperty)
    {
        if (schemaProperty.Item is not { } item)
        {
            return schemaProperty;
        }

        if (item is JsonSchemaProperty property)
        {
            return property;
        }

        var actualItem = item.ActualSchema;
        var itemSchemaProperty = new JsonSchemaProperty
        {
            Type = actualItem.Type,
            Format = actualItem.Format,
            Item = actualItem.Item
        };
        foreach (var itemProperty in actualItem.GetFlattenedProperties())
        {
            itemSchemaProperty.Properties[itemProperty.Name] = itemProperty;
        }

        return itemSchemaProperty;
    }

    void ThrowIfUnsupportedEventValueExpression(JsonSchemaProperty targetSchemaProperty, string expression, IEventValueProviderExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            logger.UnsupportedEventValueExpression(expression, targetSchemaProperty.Type);
            throw new UnsupportedEventValueExpression(targetSchemaProperty, expression);
        }
    }

    object Convert(JsonSchemaProperty schemaProperty, object input)
    {
        if (input is null)
        {
            return null!;
        }

        if (ConvertTimestamp(schemaProperty, input) is { } convertedTimestamp)
        {
            return convertedTimestamp;
        }

        if (input is string text && schemaProperty.Type == JsonObjectType.String)
        {
            return ConvertText(schemaProperty, text);
        }

        if (input is ExpandoObject)
        {
            return ConvertObject(schemaProperty, (input as IDictionary<string, object>)!);
        }

        var targetType = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);
        if (targetType is not null)
        {
            return DecimalValues.ConvertTo(targetType, input);
        }

        return ConvertCollection(schemaProperty, input);
    }

    object? ConvertTimestamp(JsonSchemaProperty schemaProperty, object input)
    {
        if (input is not DateTimeOffset timestamp || !schemaProperty.Type.HasFlag(JsonObjectType.String))
        {
            return null;
        }

        var timestampTargetType = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);

        // Date and time parts follow the same UTC instant as DateTime, not the source offset's local clock.
        return timestampTargetType switch
        {
            _ when timestampTargetType == typeof(DateTime) => timestamp.UtcDateTime,
            _ when timestampTargetType == typeof(DateTimeOffset) => timestamp,
            _ when timestampTargetType == typeof(DateOnly) => DateOnly.FromDateTime(timestamp.UtcDateTime),
            _ when timestampTargetType == typeof(TimeOnly) => TimeOnly.FromDateTime(timestamp.UtcDateTime),
            _ when timestampTargetType == typeof(string) => timestamp.ToString("O", CultureInfo.InvariantCulture),
            _ => null
        };
    }

    object ConvertText(JsonSchemaProperty schemaProperty, string text)
    {
        // A value that cannot be converted is left as-is rather than thrown over: historical
        // events can legitimately carry values no current type reads (renamed identifiers,
        // deleted lookups), and a read-side key resolver must survive them. Conversions that
        // already succeed keep working; only the hard TypeConversion.Convert failure - a
        // FormatException, InvalidCast or NotSupported shape - degrades to the raw value.
        // The value itself is never logged: event content can carry personal or otherwise protected
        // data, and exception messages from conversions can echo the input, so only the property
        // name, the target type and the kind of failure are recorded.
        var targetType = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(string);
        try
        {
            return TypeConversion.Convert(targetType, text);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or NotSupportedException)
        {
            logger.EventValueLeftUnconverted(schemaProperty.Name, targetType.Name, ex.GetType().Name);
            return text;
        }
    }

    object ConvertObject(JsonSchemaProperty schemaProperty, IDictionary<string, object> expandoObject)
    {
        if (TryGetWrappedScalarValue(expandoObject, schemaProperty, typeFormats, out var conceptValue))
        {
            return Convert(schemaProperty, conceptValue!);
        }

        var properties = schemaProperty.IsArray ?
            schemaProperty.Item!.GetFlattenedProperties() :
            schemaProperty.GetFlattenedProperties();

        foreach (var property in properties)
        {
            // A value does not necessarily carry every property declared by the schema. Only convert
            // properties that are actually present on the value; absent ones are simply not part of
            // this concrete shape and must not be accessed by indexer (which would throw).
            if (expandoObject.TryGetValue(property.Name, out var propertyValue))
            {
                expandoObject[property.Name] = Convert(property, propertyValue);
            }
        }

        return expandoObject;
    }

    object ConvertCollection(JsonSchemaProperty schemaProperty, object input)
    {
        var inputType = input.GetType();
        if (IsDictionary(input, inputType) || !inputType.IsEnumerable())
        {
            return input;
        }

        var itemSchemaProperty = GetItemSchemaProperty(schemaProperty);
        var children = new List<object>();
        foreach (var child in (input as IEnumerable)!)
        {
            children.Add(Convert(itemSchemaProperty, child));
        }

        return children;
    }
}
