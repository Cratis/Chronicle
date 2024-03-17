// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Events;
using Cratis.Properties;
using Cratis.Reflection;
using Cratis.Schemas;
using Cratis.Types;
using NJsonSchema;

namespace Cratis.Kernel.Projections.Expressions.EventValues;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ITypeFormats"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for finding target types.</param>
public class EventValueProviderExpressionResolvers(ITypeFormats typeFormats) : IEventValueProviderExpressionResolvers
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
            var properties = schemaProperty.IsArray ?
                schemaProperty.Item.GetFlattenedProperties() :
                schemaProperty.GetFlattenedProperties();

            foreach (var property in properties)
            {
                expandoObject[property.Name] = Convert(property, expandoObject[property.Name]);
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
