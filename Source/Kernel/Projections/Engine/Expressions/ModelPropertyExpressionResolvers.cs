// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Projections.Expressions.ModelProperties;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions;

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
    public ModelPropertyExpressionResolvers(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers)
    {
        _eventValueProviderExpressionResolvers = eventValueProviderExpressionResolvers;

        _resolvers = new IModelPropertyExpressionResolver[]
            {
                new AddExpressionResolver(_eventValueProviderExpressionResolvers),
                new SubtractExpressionResolver(_eventValueProviderExpressionResolvers),
                new CountExpressionResolver(),
                new SetExpressionResolver(_eventValueProviderExpressionResolvers)
            };
    }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _resolvers.Any(_ => _.CanResolve(targetProperty, expression));

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetProperty, expression));
        ThrowIfUnsupportedModelPropertyExpression(expression, resolver);
        return resolver!.Resolve(targetProperty, expression);
    }

    void ThrowIfUnsupportedModelPropertyExpression(string expression, IModelPropertyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedModelPropertyExpression(expression);
        }
    }
}
