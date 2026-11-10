// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Numerics;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Types;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents utilities for creating <see cref="PropertyMapper{Event, ExpandoObject}"/> for different scenarios.
/// </summary>
public static class PropertyMappers
{
    const string RoundTripDateTimeKeyFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss','fffffffK";

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can copies content provided by a <see cref="ValueProvider{Event}"/> to a target property.
    /// </summary>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> FromEventValueProvider(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (ResolveDynamicPropertyPath(targetProperty, @event) is not { } resolvedProperty)
            {
                return NoChange(targetProperty, arrayIndexers);
            }

            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            var property = resolvedProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget[property] = StateValues.Copy(eventValueProvider(@event));
            return new(resolvedProperty, originalValue, newValue, arrayIndexers);
        };
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that copies content provided by a <see cref="ValueProvider{Event}"/> to a target property,
    /// but only when the target property has no value yet - it is missing or null.
    /// </summary>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    /// <remarks>
    /// When the target already holds a value, nothing is changed and no difference is produced. Since the decision only
    /// depends on the state built from earlier events, it is deterministic on replay.
    /// </remarks>
    public static PropertyMapper<AppendedEvent, ExpandoObject> SetOnceFromEventValueProvider(PropertyPath targetProperty, ValueProvider<AppendedEvent> eventValueProvider)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (ResolveDynamicPropertyPath(targetProperty, @event) is not { } resolvedProperty)
            {
                return NoChange(targetProperty, arrayIndexers);
            }

            var existingParent = target.TryGetExistingPath(resolvedProperty, arrayIndexers) as IDictionary<string, object?>;
            if (existingParent?.TryGetValue(resolvedProperty.LastSegment.Value, out var existing) == true && existing is not null)
            {
                return NoChange(resolvedProperty, arrayIndexers);
            }

            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            var property = resolvedProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget[property] = StateValues.Copy(eventValueProvider(@event));
            return new(resolvedProperty, originalValue, newValue, arrayIndexers);
        };
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that removes a dictionary key addressed by a dynamic key segment.
    /// </summary>
    /// <param name="targetProperty">Target property, holding a dynamic key segment.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    /// <remarks>
    /// The key is removed entirely, not set to null. Removing a key that is not present is a no-op.
    /// </remarks>
    public static PropertyMapper<AppendedEvent, ExpandoObject> Remove(PropertyPath targetProperty)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (ResolveDynamicPropertyPath(targetProperty, @event) is not { } resolvedProperty ||
                target.TryGetExistingPath(resolvedProperty, arrayIndexers) is not IDictionary<string, object?> parent ||
                !parent.Remove(resolvedProperty.LastSegment.Value, out var original))
            {
                return NoChange(targetProperty, arrayIndexers);
            }

            return PropertyDifference.Removal(resolvedProperty, original, arrayIndexers);
        };
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can add a property with a value provided by a <see cref="ValueProvider{Event}"/> onto a target property.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use.</param>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> AddWithEventValueProvider(ITypeFormats typeFormats, PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, ValueProvider<AppendedEvent> eventValueProvider)
    {
        var targetType = targetPropertySchema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(double);
        return Accumulate(targetProperty, targetType, (current, @event) => PerformAdd(targetType, current, eventValueProvider(@event)));
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can subtract a property with a value provided by a <see cref="ValueProvider{Event}"/> from a target property.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use.</param>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <param name="eventValueProvider"><see cref="ValueProvider{Event}"/> to use as source.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> SubtractWithEventValueProvider(ITypeFormats typeFormats, PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, ValueProvider<AppendedEvent> eventValueProvider)
    {
        var targetType = targetPropertySchema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(double);
        return Accumulate(targetProperty, targetType, (current, @event) => PerformSubtract(targetType, current, eventValueProvider(@event)));
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can count by increasing the target property when called.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use.</param>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> Count(ITypeFormats typeFormats, PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema)
    {
        var targetType = targetPropertySchema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(int);
        return Accumulate(targetProperty, targetType, (current, _) => PerformAdd(targetType, current, 1));
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can increment the target property when called.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use.</param>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> Increment(ITypeFormats typeFormats, PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema)
    {
        var targetType = targetPropertySchema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(int);
        return Accumulate(targetProperty, targetType, (current, _) => PerformAdd(targetType, current, 1));
    }

    /// <summary>
    /// Create a <see cref="PropertyMapper{Event, ExpandoObject}"/> that can decrement the target property when called.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use.</param>
    /// <param name="targetProperty">Target property.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <returns>A new <see cref="PropertyMapper{Event, ExpandoObject}"/>.</returns>
    public static PropertyMapper<AppendedEvent, ExpandoObject> Decrement(ITypeFormats typeFormats, PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema)
    {
        var targetType = targetPropertySchema.GetTargetTypeForJsonSchemaProperty(typeFormats) ?? typeof(int);
        return Accumulate(targetProperty, targetType, (current, _) => PerformSubtract(targetType, current, 1));
    }

    static PropertyMapper<AppendedEvent, ExpandoObject> Accumulate(PropertyPath targetProperty, Type targetType, Func<object, AppendedEvent, object> accumulate)
    {
        return (@event, target, arrayIndexers) =>
        {
            if (ResolveDynamicPropertyPath(targetProperty, @event) is not { } resolvedProperty)
            {
                return NoChange(targetProperty, arrayIndexers);
            }

            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var result = accumulate(valueAsObject, @event);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
    }

    static PropertyDifference NoChange(PropertyPath propertyPath, ArrayIndexers arrayIndexers) => new(propertyPath, null, null, arrayIndexers);

    static object PerformAdd(Type targetType, object currentValue, object valueToAdd)
    {
        return targetType switch
        {
            _ when targetType == typeof(TimeSpan) => (TimeSpan)currentValue + (TimeSpan)valueToAdd,
            _ when targetType == typeof(double) => Add((double)Convert.ChangeType(currentValue, typeof(double)), (double)Convert.ChangeType(valueToAdd, typeof(double))),
            _ when targetType == typeof(float) => Add((float)Convert.ChangeType(currentValue, typeof(float)), (float)Convert.ChangeType(valueToAdd, typeof(float))),
            _ when targetType == typeof(decimal) => Add((decimal)Convert.ChangeType(currentValue, typeof(decimal)), (decimal)Convert.ChangeType(valueToAdd, typeof(decimal))),
            _ when targetType == typeof(long) => Add((long)Convert.ChangeType(currentValue, typeof(long)), (long)Convert.ChangeType(valueToAdd, typeof(long))),
            _ when targetType == typeof(int) => Add((int)Convert.ChangeType(currentValue, typeof(int)), (int)Convert.ChangeType(valueToAdd, typeof(int))),
            _ when targetType == typeof(short) => Add((short)Convert.ChangeType(currentValue, typeof(short)), (short)Convert.ChangeType(valueToAdd, typeof(short))),
            _ when targetType == typeof(byte) => Add((byte)Convert.ChangeType(currentValue, typeof(byte)), (byte)Convert.ChangeType(valueToAdd, typeof(byte))),
            _ => TypeConversion.Convert(targetType, Add((double)Convert.ChangeType(currentValue, typeof(double)), (double)Convert.ChangeType(valueToAdd, typeof(double))))
        };
    }

    static object PerformSubtract(Type targetType, object currentValue, object valueToSubtract)
    {
        return targetType switch
        {
            _ when targetType == typeof(TimeSpan) => (TimeSpan)currentValue - (TimeSpan)valueToSubtract,
            _ when targetType == typeof(double) => Subtract((double)Convert.ChangeType(currentValue, typeof(double)), (double)Convert.ChangeType(valueToSubtract, typeof(double))),
            _ when targetType == typeof(float) => Subtract((float)Convert.ChangeType(currentValue, typeof(float)), (float)Convert.ChangeType(valueToSubtract, typeof(float))),
            _ when targetType == typeof(decimal) => Subtract((decimal)Convert.ChangeType(currentValue, typeof(decimal)), (decimal)Convert.ChangeType(valueToSubtract, typeof(decimal))),
            _ when targetType == typeof(long) => Subtract((long)Convert.ChangeType(currentValue, typeof(long)), (long)Convert.ChangeType(valueToSubtract, typeof(long))),
            _ when targetType == typeof(int) => Subtract((int)Convert.ChangeType(currentValue, typeof(int)), (int)Convert.ChangeType(valueToSubtract, typeof(int))),
            _ when targetType == typeof(short) => Subtract((short)Convert.ChangeType(currentValue, typeof(short)), (short)Convert.ChangeType(valueToSubtract, typeof(short))),
            _ when targetType == typeof(byte) => Subtract((byte)Convert.ChangeType(currentValue, typeof(byte)), (byte)Convert.ChangeType(valueToSubtract, typeof(byte))),
            _ => TypeConversion.Convert(targetType, Subtract((double)Convert.ChangeType(currentValue, typeof(double)), (double)Convert.ChangeType(valueToSubtract, typeof(double))))
        };
    }

    static T Add<T>(T left, T right)
        where T : INumber<T> => left + right;

    static T Subtract<T>(T left, T right)
        where T : INumber<T> => left - right;

    /// <summary>
    /// Resolve a property path that may contain a dynamic, expression-based segment (e.g. eventCountByType.$eventContext.eventType.id)
    /// into a concrete <see cref="PropertyPath"/> for the current <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="propertyPath">The property path that may contain a .$eventContext.&lt;path&gt;, .$eventContent.&lt;path&gt; or .$causedBy.&lt;property&gt; segment.</param>
    /// <param name="event">The event to evaluate the dynamic segment against.</param>
    /// <returns>
    /// The resolved property path, with the dynamic segment replaced by its runtime value. Returns <paramref name="propertyPath"/> unchanged when it has no dynamic segment.
    /// Returns null when a key read from the event content is missing or null - the operation is then skipped rather than written under an empty key.
    /// </returns>
    /// <exception cref="UnsupportedDynamicPropertyPathExpression">Thrown when the dynamic segment does not reference a well-known expression this method knows how to resolve.</exception>
    static PropertyPath? ResolveDynamicPropertyPath(PropertyPath propertyPath, AppendedEvent @event)
    {
        var pathString = propertyPath.Path;
        var markerIndex = pathString.IndexOf(".$", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return propertyPath;
        }

        var staticPrefix = pathString[..markerIndex];
        var expressionStart = markerIndex + 1;
        var propertyStart = pathString.IndexOf('.', expressionStart);
        if (propertyStart < 0 || propertyStart == pathString.Length - 1)
        {
            throw new UnsupportedDynamicPropertyPathExpression(propertyPath);
        }

        var expression = pathString[expressionStart..propertyStart];
        var property = pathString[(propertyStart + 1)..];
        if (expression == WellKnownExpressions.EventContent)
        {
            var key = ReadExistingEventContentValue(@event.Content, new PropertyPath(property));
            return key is null ? null : new PropertyPath($"{staticPrefix}.{EventContentDictionaryKeyFor(propertyPath, key)}");
        }

        var contextPath = expression switch
        {
            WellKnownExpressions.EventContext => property,
            WellKnownExpressions.CausedBy when property.Equals("subject", StringComparison.Ordinal) || property.Equals("name", StringComparison.Ordinal) || property.Equals("userName", StringComparison.Ordinal) => $"causedBy.{property}",
            _ => throw new UnsupportedDynamicPropertyPathExpression(propertyPath)
        };
        var resolvedValue = EventValueProviders.EventContext(new PropertyPath(contextPath))(@event);

        // Keys from the event context and the causing identity predate key validation: keep forming them the way they
        // always have, so existing read models keep their keys and a name holding a '.' does not start failing events.
        return resolvedValue is null ? null : new PropertyPath($"{staticPrefix}.{FormatEventContextKeyValue(resolvedValue)}");
    }

    /// <summary>
    /// Format a resolved event-context value as a dictionary key the way it has always been formatted.
    /// </summary>
    /// <remarks>
    /// The value is formatted with the invariant culture, giving the same key on every host and on replay. That is the
    /// key earlier versions produced when the kernel ran with the invariant culture.
    /// </remarks>
    /// <param name="value">The resolved value.</param>
    /// <returns>The dictionary key.</returns>
    static string FormatEventContextKeyValue(object value) => value switch
    {
        _ when value.IsConcept() => value.GetConceptValue() is { } conceptValue ? FormatEventContextKeyValue(conceptValue) : string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>
    /// Format a resolved event-content value as a dictionary key, refusing a key that cannot be stored as one.
    /// </summary>
    /// <param name="propertyPath">The property path holding the dynamic segment, for reporting.</param>
    /// <param name="value">The resolved value.</param>
    /// <returns>The dictionary key.</returns>
    /// <exception cref="InvalidDictionaryKey">Thrown when the key is empty, holds a '.', '[', ']' or a null character, or starts with '$'.</exception>
    static string EventContentDictionaryKeyFor(PropertyPath propertyPath, object? value)
    {
        var key = FormatDynamicKeyValue(value);
        if (key.Length == 0 || key.AsSpan().IndexOfAny(".[]\0") >= 0 || key.StartsWith('$'))
        {
            throw new InvalidDictionaryKey(propertyPath, key);
        }

        return key;
    }

    /// <summary>
    /// Read a value from the event content without creating anything along the path - the content is shared and must not be changed.
    /// </summary>
    /// <param name="content">The event content.</param>
    /// <param name="path">The path to read.</param>
    /// <returns>The value, or null when it or any ancestor along the path is missing.</returns>
    static object? ReadExistingEventContentValue(ExpandoObject content, PropertyPath path)
    {
        var parent = content.TryGetExistingPath(path, ArrayIndexers.NoIndexers) as IDictionary<string, object?>;
        return parent?.TryGetValue(path.LastSegment.Value, out var value) == true ? value : null;
    }

    /// <summary>
    /// Format a resolved dynamic-key value as the <see cref="string"/> to use for a dictionary key, unwrapping
    /// any <see cref="ConceptAs{T}"/> value generically rather than special-casing individual concept types.
    /// </summary>
    /// <remarks>
    /// The key is formatted independently of the culture the kernel runs with, so the same event gives the same key on
    /// every host and on replay. Dates and times use the ISO 8601 round-trip format with a comma as the decimal sign,
    /// which ISO 8601 allows, since a '.' cannot be part of a dictionary key.
    /// </remarks>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted key.</returns>
    static string FormatDynamicKeyValue(object? value) => value switch
    {
        null => string.Empty,
        _ when value.IsConcept() => FormatDynamicKeyValue(value.GetConceptValue()),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(RoundTripDateTimeKeyFormat, CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString(RoundTripDateTimeKeyFormat, CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.ToString("HH':'mm':'ss','fffffff", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
