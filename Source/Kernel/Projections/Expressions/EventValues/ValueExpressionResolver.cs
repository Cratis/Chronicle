// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Events;
using Cratis.Properties;

namespace Cratis.Kernel.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for resolving value to a constant.
/// </summary>
public class ValueExpressionResolver : IEventValueProviderExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$value\\((?<value>[\\w ._/:\\*\\+\\-]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var match = _regularExpression.Match(expression);
        return EventValueProviders.Value(match.Groups["value"].Value);
    }
}
