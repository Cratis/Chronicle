// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

/// <summary>
/// Represents an implementation of <see cref="IKeyExpressionResolver"/> for composite key expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositeKeyExpressionResolver"/> class.
/// </remarks>
/// <param name="resolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving event values.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers" /> for resolving the <see cref="Key"/>.</param>
public partial class CompositeKeyExpressionResolver(IEventValueProviderExpressionResolvers resolvers, IKeyResolvers keyResolvers) : IKeyExpressionResolver
{
    static readonly Regex _regularExpression = CompositeKeyRegEx();

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

            var schemaProperty = projection.ReadModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(actualProperty)!;
            schemaProperty ??= new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            return new
            {
                Property = new PropertyPath(keyValue[0]),
                KeyResolver = resolvers.Resolve(schemaProperty, keyValue[1])
            };
        }).ToDictionary(_ => _.Property, _ => _.KeyResolver);

        return keyResolvers.Composite(propertiesWithKeyValueProviders);
    }

    [GeneratedRegex("\\$composite\\((?<expressions>[\\w=$\\({\\)., ]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CompositeKeyRegEx();
}
