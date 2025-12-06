// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IReadModelPropertyExpressionResolvers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeyExpressionResolvers"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event value expressions.</param>
/// <param name="defaultKeyResolvers"><see cref="IKeyResolvers" /> for resolving the <see cref="Key"/>.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public partial class KeyExpressionResolvers(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers, IKeyResolvers defaultKeyResolvers, ILogger<KeyExpressionResolvers> logger) : IKeyExpressionResolvers
{
    readonly IKeyExpressionResolver[] _resolvers =
        [
            new CompositeKeyExpressionResolver(eventValueProviderExpressionResolvers, defaultKeyResolvers),
            new EventValueKeyExpressionResolver(eventValueProviderExpressionResolvers, defaultKeyResolvers)
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

    /// <inheritdoc/>
    public KeyResolver ResolveWithFallbackToEventSourceId(IProjection projection, string expression, PropertyPath identifiedByProperty, IKeyResolvers keyResolvers)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(expression));
        ThrowIfUnsupportedKeyExpression(expression, resolver);

        // For event value expressions, create a value provider that returns the value or null
        // Then use the fallback key resolver
        var schemaProperty = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(identifiedByProperty);
        schemaProperty ??= new JsonSchemaProperty { Type = JsonObjectType.String };
        var valueProvider = eventValueProviderExpressionResolvers.Resolve(schemaProperty, expression);

        return keyResolvers.FromEventValueProviderWithFallbackToEventSourceId(valueProvider);
    }

    void ThrowIfUnsupportedKeyExpression(string expression, IKeyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            logger.UnsupportedKeyExpression(expression);
            throw new UnsupportedKeyExpression(expression);
        }
    }
}
