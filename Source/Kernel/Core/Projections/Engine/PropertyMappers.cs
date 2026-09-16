// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Numerics;
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
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            var property = resolvedProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget[property] = eventValueProvider(@event);
            return new(resolvedProperty, originalValue, newValue, arrayIndexers);
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

        return (@event, target, arrayIndexers) =>
        {
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var eventValueObject = eventValueProvider(@event);
            var result = PerformAdd(targetType, valueAsObject, eventValueObject);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
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

        return (@event, target, arrayIndexers) =>
        {
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var eventValueObject = eventValueProvider(@event);
            var result = PerformSubtract(targetType, valueAsObject, eventValueObject);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
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

        return (@event, target, arrayIndexers) =>
        {
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var result = PerformAdd(targetType, valueAsObject, 1);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
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

        return (@event, target, arrayIndexers) =>
        {
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var result = PerformAdd(targetType, valueAsObject, 1);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
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

        return (@event, target, arrayIndexers) =>
        {
            var resolvedProperty = ResolveDynamicPropertyPath(targetProperty, @event);
            var lastSegment = resolvedProperty.LastSegment;
            var actualTarget = target.EnsurePath(resolvedProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }

            var result = PerformSubtract(targetType, valueAsObject, 1);
            actualTarget[lastSegment.Value] = result;
            return new(resolvedProperty, valueAsObject, result, arrayIndexers);
        };
    }

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
    /// <param name="propertyPath">The property path that may contain a .$eventContext.&lt;path&gt; segment.</param>
    /// <param name="event">The event to evaluate the dynamic segment against.</param>
    /// <returns>The resolved property path, with the dynamic segment replaced by its runtime value. Returns <paramref name="propertyPath"/> unchanged when it has no dynamic segment.</returns>
    /// <exception cref="UnsupportedDynamicPropertyPathExpression">Thrown when the dynamic segment does not reference a well-known expression this method knows how to resolve.</exception>
    static PropertyPath ResolveDynamicPropertyPath(PropertyPath propertyPath, AppendedEvent @event)
    {
        var pathString = propertyPath.Path;
        var eventContextMarker = $".{WellKnownExpressions.EventContext}.";
        var markerIndex = pathString.IndexOf(eventContextMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return propertyPath;
        }

        var staticPrefix = pathString[..markerIndex];
        var contextPath = pathString[(markerIndex + eventContextMarker.Length)..];
        if (contextPath.Length == 0)
        {
            throw new UnsupportedDynamicPropertyPathExpression(propertyPath);
        }

        var contextPropertyPath = new PropertyPath(contextPath);
        var resolvedValue = EventValueProviders.EventContext(contextPropertyPath)(@event);

        return new PropertyPath($"{staticPrefix}.{FormatDynamicKeyValue(resolvedValue)}");
    }

    /// <summary>
    /// Format a resolved dynamic-key value as the <see cref="string"/> to use for a dictionary key, unwrapping
    /// any <see cref="ConceptAs{T}"/> value generically rather than special-casing individual concept types.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted key.</returns>
    static string FormatDynamicKeyValue(object? value) => value switch
    {
        null => string.Empty,
        _ when value.IsConcept() => FormatDynamicKeyValue(value.GetConceptValue()),
        _ => value.ToString() ?? string.Empty
    };
}
