// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Projections.Expressions.Keys;

/// <summary>
/// Represents a <see cref="IKeyExpressionResolver"/> for resolving keys based on regular event value expressions.
/// </summary>
public class EventValueKeyExpressionResolver : IKeyExpressionResolver
{
    readonly IEventValueProviderExpressionResolvers _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventValueKeyExpressionResolver"/> class.
    /// </summary>
    /// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
    public EventValueKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers) => _resolvers = resolvers;

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _resolvers.CanResolve(expression);

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        var schemaProperty = projection.Model.Schema.GetSchemaPropertyForPropertyPath(identifiedByProperty)!;
        schemaProperty ??= new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        return KeyResolvers.FromEventValueProvider(_resolvers.Resolve(schemaProperty, expression));
    }
}
