// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IKeyExpressionResolver"/> for composite key expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositeKeyExpressionResolver"/> class.
/// </remarks>
/// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers" /> for resolving the <see cref="Key"/>.</param>
public class CompositeKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers, IKeyResolvers keyResolvers) : IKeyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => expression.StartsWith("$composite(", StringComparison.Ordinal);

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        CompositeKeyExpression parsed;
        try
        {
            parsed = CompositeKeyExpression.Parse(expression);
        }
        catch (InvalidCompositeKeyExpression exception)
        {
            if (expression == "$composite()" || expression == "$composite( )")
            {
                throw new MissingCompositeExpressions(projection.Identifier, identifiedByProperty, expression);
            }
            throw new InvalidCompositeKeyPropertyMappingExpression(projection.Identifier, identifiedByProperty, exception.Component);
        }

        var propertiesWithKeyValueProviders = parsed.Mappings.Select(mapping =>
        {
            var actualProperty = identifiedByProperty + mapping.Property;

            var schemaProperty = projection.ReadModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(actualProperty);
            schemaProperty ??= new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            return new
            {
                Property = new PropertyPath(mapping.Property),
                KeyResolver = resolvers.Resolve(schemaProperty, mapping.Expression)
            };
        }).ToDictionary(_ => _.Property, _ => _.KeyResolver);

        return keyResolvers.Composite(propertiesWithKeyValueProviders);
    }
}
