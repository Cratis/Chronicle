// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for resolving value to a constant.
/// </summary>
public partial class ValueExpressionResolver : IEventValueProviderExpressionResolver
{
    [GeneratedRegex("\\$value\\((?<value>[\\w ._/:\\*\\+\\-]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    static partial Regex ValueRegEx { get; }

    /// <inheritdoc/>
    public bool CanResolve(string expression) => ValueRegEx.Match(expression).Success;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var match = ValueRegEx.Match(expression);
        return EventValueProviders.Value(match.Groups["value"].Value);
    }
}
