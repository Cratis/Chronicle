// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Aksio.Cratis.Events.Projections.Expressions.EventValues;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IKeyExpressionResolver"/> for composite key expressions.
/// </summary>
public class CompositeKeyExpressionResolver : IKeyExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$composite\\((?<expressions>[\\w=$\\({\\)., ]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    readonly IEventValueProviderExpressionResolvers _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKeyExpressionResolver"/> class.
    /// </summary>
    /// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
    public CompositeKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers) => _resolvers = resolvers;

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public KeyResolver Resolve(IProjection projection, string expression, PropertyPath identifiedByProperty)
    {
        var match = _regularExpression.Match(expression);
        var rawExpressions = match.Groups["expressions"].Value;
        var expressions = rawExpressions.Split(',').Select(_ => _.Trim()).ToArray();

        if (rawExpressions.Length == 0 || expressions.Length == 0)
        {
            throw new MissingCompositeExpressions(projection.Identifier, identifiedByProperty, expression);
        }

        var propertiesWithKeyValueProviders = expressions.Select(_ =>
        {
            var keyValue = _.Split('=');
            if (keyValue.Length != 2)
            {
                throw new InvalidCompositeKeyPropertyMappingExpression(projection.Identifier, identifiedByProperty, _);
            }
            var actualProperty = identifiedByProperty + keyValue[0];

            var schemaProperty = projection.Model.Schema.GetSchemaPropertyForPropertyPath(actualProperty)!;
            schemaProperty ??= new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            return new
            {
                Property = new PropertyPath(keyValue[0]),
                KeyResolver = _resolvers.Resolve(schemaProperty, keyValue[1])
            };
        }).ToDictionary(_ => _.Property, _ => _.KeyResolver);

        return KeyResolvers.Composite(propertiesWithKeyValueProviders);
    }
}
