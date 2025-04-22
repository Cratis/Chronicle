// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.ModelProperties;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> to use for value provider resolvers.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
public class ModelPropertyExpressionResolvers(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers, ITypeFormats typeFormats) : IModelPropertyExpressionResolvers
{
    readonly IModelPropertyExpressionResolver[] _resolvers =
        [
            new AddExpressionResolver(eventValueProviderExpressionResolvers),
            new SubtractExpressionResolver(eventValueProviderExpressionResolvers),
            new CountExpressionResolver(typeFormats),
            new IncrementExpressionResolver(typeFormats),
            new DecrementExpressionResolver(typeFormats),
            new SetExpressionResolver(eventValueProviderExpressionResolvers)
        ];

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) =>
        _resolvers.Any(_ => _.CanResolve(targetProperty, expression));

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetProperty, expression));
        ThrowIfUnsupportedModelPropertyExpression(expression, resolver);
        return resolver!.Resolve(targetProperty, targetPropertySchema, expression);
    }

    static void ThrowIfUnsupportedModelPropertyExpression(string expression, IModelPropertyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedModelPropertyExpression(expression);
        }
    }
}
