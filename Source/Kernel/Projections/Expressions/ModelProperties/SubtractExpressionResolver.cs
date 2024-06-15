// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
public class SubtractExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IModelPropertyExpressionResolver
{
    static readonly Regex _regularExpression = new($"\\$subtract\\((?<expression>{EventValueProviderRegularExpressions.Expression}*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var match = _regularExpression.Match(expression);
        return PropertyMappers.SubtractWithEventValueProvider(targetProperty, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, match.Groups["expression"].Value));
    }
}
