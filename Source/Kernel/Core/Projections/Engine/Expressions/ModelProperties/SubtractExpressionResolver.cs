// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for subtracting value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubtractExpressionResolver"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
public partial class SubtractExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers, ITypeFormats typeFormats) : IReadModelPropertyExpressionResolver
{
    [GeneratedRegex($"\\{WellKnownExpressions.Subtract}\\((?<expression>{EventValueProviderRegularExpressions.Expression}*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    static partial Regex SubtractRegEx { get; }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => SubtractRegEx.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var match = SubtractRegEx.Match(expression);
        return PropertyMappers.SubtractWithEventValueProvider(typeFormats, targetProperty, targetPropertySchema, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, match.Groups["expression"].Value));
    }
}
