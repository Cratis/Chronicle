// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IKeyExpressionResolver"/> for flat composite key expressions.
/// </summary>
/// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers" /> for creating the <see cref="KeyResolver"/>.</param>
public class FlatCompositeKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers, IKeyResolvers keyResolvers) : IKeyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => FlatCompositeKeyExpression.IsFlatComposite(expression);

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        if (projection.HasParent)
        {
            throw new InvalidFlatCompositeKeyExpression(expression, "a flat composite key identifies a root read model and cannot be used for a child projection");
        }

        var parsed = FlatCompositeKeyExpression.Parse(expression);
        var schema = projection.ReadModel.GetSchemaForLatestGeneration();
        var providers = parsed.Parts.Select(part =>
        {
            var schemaProperty = schema.GetSchemaPropertyForPropertyPath(new PropertyPath(part.Property))
                ?? throw new InvalidFlatCompositeKeyExpression(expression, $"part '{part.Property}' is not a property of the read model");
            if (schemaProperty.IsArray || schemaProperty.Type.HasFlag(JsonObjectType.Object))
            {
                throw new InvalidFlatCompositeKeyExpression(expression, $"part '{part.Property}' must be a scalar property of the read model");
            }

            return (part.Property, Provider: resolvers.Resolve(schemaProperty, part.Expression));
        }).ToArray();

        return keyResolvers.FlatComposite(providers);
    }
}
