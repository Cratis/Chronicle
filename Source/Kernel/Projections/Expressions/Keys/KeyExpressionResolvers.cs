// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Projections.Expressions.EventValues;
using Cratis.Properties;

namespace Cratis.Kernel.Projections.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertyExpressionResolvers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeyExpressionResolvers"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event value expressions.</param>
public class KeyExpressionResolvers(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IKeyExpressionResolvers
{
    readonly IKeyExpressionResolver[] _resolvers =
        [
            new CompositeKeyExpressionResolver(eventValueProviderExpressionResolvers),
            new EventValueKeyExpressionResolver(eventValueProviderExpressionResolvers)
        ];

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _resolvers.Any(_ => _.CanResolve(expression));

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
        ThrowIfUnsupportedKeyExpression(expression, resolver);
        return resolver!.Resolve(projection, expression, identifiedByProperty);
    }

    void ThrowIfUnsupportedKeyExpression(string expression, IKeyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            throw new UnsupportedKeyExpression(expression);
        }
    }
}
