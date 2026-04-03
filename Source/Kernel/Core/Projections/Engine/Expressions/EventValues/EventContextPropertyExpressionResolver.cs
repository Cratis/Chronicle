// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for resolving value from <see cref="EventSourceId"/> of the <see cref="AppendedEvent"/>.
/// </summary>
public partial class EventContextPropertyExpressionResolver : IEventValueProviderExpressionResolver
{
    [GeneratedRegex("\\$eventContext\\((?<property>[A-Za-z.]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    static partial Regex EventContextRegEx { get; }

    /// <inheritdoc/>
    public bool CanResolve(string expression) => EventContextRegEx.Match(expression).Success;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var match = EventContextRegEx.Match(expression);
        return EventValueProviders.EventContext(match.Groups["property"].Value);
    }
}
