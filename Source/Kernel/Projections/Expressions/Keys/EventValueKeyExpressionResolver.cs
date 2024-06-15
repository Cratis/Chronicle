// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Properties;
using Cratis.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

/// <summary>
/// Represents a <see cref="IKeyExpressionResolver"/> for resolving keys based on regular event value expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventValueKeyExpressionResolver"/> class.
/// </remarks>
/// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
public class EventValueKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers) : IKeyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => resolvers.CanResolve(expression);

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        var schemaProperty = projection.Model.Schema.GetSchemaPropertyForPropertyPath(identifiedByProperty)!;
        schemaProperty ??= new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        return KeyResolvers.FromEventValueProvider(resolvers.Resolve(schemaProperty, expression));
    }
}
