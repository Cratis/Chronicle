// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

/// <summary>
/// Represents a <see cref="IKeyExpressionResolver"/> for resolving keys based on regular event value expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventValueKeyExpressionResolver"/> class.
/// </remarks>
/// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers" /> for resolving the <see cref="Key"/>.</param>
public class EventValueKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers, IKeyResolvers keyResolvers) : IKeyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => resolvers.CanResolve(expression);

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        var schemaProperty = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(identifiedByProperty)!;
        schemaProperty ??= new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        return keyResolvers.FromEventValueProvider(resolvers.Resolve(schemaProperty, expression));
    }
}
