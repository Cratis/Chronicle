// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for resolving value from <see cref="EventSourceId"/> of the <see cref="AppendedEvent"/>.
/// </summary>
public partial class EventContextPropertyExpressionResolver : IEventValueProviderExpressionResolver
{
    static readonly Regex _regularExpression = EventContextRegEx();

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var match = _regularExpression.Match(expression);
        return EventValueProviders.EventContext(match.Groups["property"].Value);
    }

    [GeneratedRegex("\\$eventContext\\((?<property>[A-Za-z.]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EventContextRegEx();
}
