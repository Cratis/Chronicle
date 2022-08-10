// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IPropertyMapperExpressionResolvers"/>.
/// </summary>
public class PropertyMapperExpressionResolvers : IPropertyMapperExpressionResolvers
{
    readonly IPropertyMapperExpressionResolver[] _resolvers = new IPropertyMapperExpressionResolver[]
    {
            new EventSourceIdExpressionResolver(),
            new EventContextPropertyExpressionResolver(),
            new AddExpressionResolver(),
            new SubtractExpressionResolver(),
            new CountExpressionResolver(),
            new PropertyOnEventContentExpressionProvider()
    };

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _resolvers.Any(_ => _.CanResolve(targetProperty, expression));

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetProperty, expression));
        ThrowIfUnsupportedEventValueExpression(expression, resolver);
        return resolver!.Resolve(targetProperty, expression);
    }

    void ThrowIfUnsupportedEventValueExpression(string expression, IPropertyMapperExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedPropertyMapperExpression(expression);
        }
    }
}
