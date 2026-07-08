// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
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
        new EventContextPropertyExpressionResolver(),
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

        if (input is ExpandoObject)
        {
            var expandoObject = (input as IDictionary<string, object>)!;
            if (expandoObject.Count == 1 &&
                expandoObject.TryGetValue("value", out var conceptValue))
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

        var targetType = schemaProperty.GetTargetTypeForJsonSchemaProperty(typeFormats);
        if (targetType is not null)
        {
            return TypeConversion.Convert(targetType, input);
        }

        if (input.GetType().IsEnumerable())
        {
            var children = new List<object>();
            foreach (var child in (input as IEnumerable)!)
            {
                children.Add(Convert(schemaProperty, child));
            }

            return children;
        }

        return input;
    }
}
