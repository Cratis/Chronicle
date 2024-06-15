// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.ModelProperties;
using Cratis.Events;
using Cratis.Properties;
using Cratis.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
public class ModelPropertyExpressionResolvers : IModelPropertyExpressionResolvers
{
    readonly IModelPropertyExpressionResolver[] _resolvers;
    readonly IEventValueProviderExpressionResolvers _eventValueProviderExpressionResolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPropertyExpressionResolvers"/> class.
    /// </summary>
    /// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> to use for value provider resolvers.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
    public ModelPropertyExpressionResolvers(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers, ITypeFormats typeFormats)
    {
        _eventValueProviderExpressionResolvers = eventValueProviderExpressionResolvers;

        _resolvers =
            [
                new AddExpressionResolver(_eventValueProviderExpressionResolvers),
                new SubtractExpressionResolver(_eventValueProviderExpressionResolvers),
                new CountExpressionResolver(typeFormats),
                new SetExpressionResolver(_eventValueProviderExpressionResolvers)
            ];
    }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _resolvers.Any(_ => _.CanResolve(targetProperty, expression));

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetProperty, expression));
        ThrowIfUnsupportedModelPropertyExpression(expression, resolver);
        return resolver!.Resolve(targetProperty, targetPropertySchema, expression);
    }

    void ThrowIfUnsupportedModelPropertyExpression(string expression, IModelPropertyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedModelPropertyExpression(expression);
        }
    }
}
