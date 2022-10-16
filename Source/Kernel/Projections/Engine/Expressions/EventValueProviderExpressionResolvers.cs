// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Expressions.EventValues;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
public class EventValueProviderExpressionResolvers : IEventValueProviderExpressionResolvers
{
    readonly IEventValueProviderExpressionResolver[] _resolvers = new IEventValueProviderExpressionResolver[]
    {
        new EventSourceIdExpressionResolver(),
        new EventContextPropertyExpressionResolver(),
        new EventContentExpressionProvider()
    };

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _resolvers.Any(_ => _.CanResolve(expression));

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
        ThrowIfUnsupportedEventValueExpression(expression, resolver);
        return resolver!.Resolve(expression);
    }

    void ThrowIfUnsupportedEventValueExpression(string expression, IEventValueProviderExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedEventValueExpression(expression);
        }
    }
}
