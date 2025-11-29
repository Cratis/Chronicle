// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using NJsonSchema;

namespace Cratis.Chronicle.Projections;

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
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            var property = targetProperty.LastSegment.Value;
            var originalValue = actualTarget.TryGetValue(property, out var value) ? value : null;
            var newValue = actualTarget![property] = eventValueProvider(@event);
            return new(targetProperty, originalValue, newValue, arrayIndexers);
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
            var lastSegment = targetProperty.LastSegment;
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget!.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }
            var currentValue = Convert.ToDecimal(valueAsObject);
            var originalValue = currentValue;
            var eventValue = Convert.ToDecimal(eventValueProvider(@event));
            var result = currentValue + eventValue;
            actualTarget[lastSegment.Value] = TypeConversion.Convert(targetType, result);
            return new(targetProperty, originalValue, result, arrayIndexers);
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
            var lastSegment = targetProperty.LastSegment;
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget!.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }
            var currentValue = Convert.ToDecimal(valueAsObject);
            var originalValue = currentValue;
            var eventValue = Convert.ToDecimal(eventValueProvider(@event));
            var result = currentValue - eventValue;
            actualTarget[lastSegment.Value] = TypeConversion.Convert(targetType, result);
            return new(targetProperty, originalValue, result, arrayIndexers);
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
            var lastSegment = targetProperty.LastSegment;
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget!.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }
            var currentValue = Convert.ToDecimal(valueAsObject);
            var originalValue = currentValue;
            var result = currentValue + 1;
            actualTarget[lastSegment.Value] = TypeConversion.Convert(targetType, result);
            return new(targetProperty, originalValue, result, arrayIndexers);
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
            var lastSegment = targetProperty.LastSegment;
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget!.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }
            var currentValue = Convert.ToDecimal(valueAsObject);
            var originalValue = currentValue;
            var result = currentValue + 1;
            actualTarget[lastSegment.Value] = TypeConversion.Convert(targetType, result);
            return new(targetProperty, originalValue, result, arrayIndexers);
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
            var lastSegment = targetProperty.LastSegment;
            var actualTarget = target.EnsurePath(targetProperty, arrayIndexers) as IDictionary<string, object>;
            if (!actualTarget!.TryGetValue(lastSegment.Value, out var valueAsObject))
            {
                valueAsObject = TypeConversion.Convert(targetType, 0);
                actualTarget[lastSegment.Value] = valueAsObject;
            }
            var currentValue = Convert.ToDecimal(valueAsObject);
            var originalValue = currentValue;
            var result = currentValue - 1;
            actualTarget[lastSegment.Value] = TypeConversion.Convert(targetType, result);
            return new(targetProperty, originalValue, result, arrayIndexers);
        };
    }
}
