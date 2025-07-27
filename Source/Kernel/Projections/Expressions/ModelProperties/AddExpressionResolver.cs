// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
public partial class AddExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IReadModelPropertyExpressionResolver
{
    static readonly Regex _regularExpression = AddRegEx();

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var match = _regularExpression.Match(expression);
        return PropertyMappers.AddWithEventValueProvider(targetProperty, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, match.Groups["expression"].Value));
    }

    [GeneratedRegex($"\\$add\\((?<expression>{EventValueProviderRegularExpressions.Expression}*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AddRegEx();
}
