// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for incrementing a property on a model.
/// </summary>
/// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
public partial class IncrementExpressionResolver(ITypeFormats typeFormats) : IReadModelPropertyExpressionResolver
{
    static readonly Regex _regularExpression = IncrementRegEx();
    readonly ITypeFormats _typeFormats = typeFormats;

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression) => PropertyMappers.Increment(_typeFormats, targetProperty, targetPropertySchema);

    [GeneratedRegex($"\\{WellKnownExpressions.Increment}", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IncrementRegEx();
}
