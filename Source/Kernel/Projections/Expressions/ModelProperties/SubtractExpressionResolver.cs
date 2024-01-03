// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
public class SubtractExpressionResolver : IModelPropertyExpressionResolver
{
    static readonly Regex _regularExpression = new($"\\$subtract\\((?<expression>{EventValueProviderRegularExpressions.Expression}*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    readonly IEventValueProviderExpressionResolvers _eventValueProviderExpressionResolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
    /// </summary>
    /// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
    public SubtractExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers)
    {
        _eventValueProviderExpressionResolvers = eventValueProviderExpressionResolvers;
    }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var match = _regularExpression.Match(expression);
        return PropertyMappers.SubtractWithEventValueProvider(targetProperty, _eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, match.Groups["expression"].Value));
    }
}
