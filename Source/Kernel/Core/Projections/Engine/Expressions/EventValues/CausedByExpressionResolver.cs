// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Resolves the identity that caused an event, or a property of that identity.
/// </summary>
public partial class CausedByExpressionResolver : IEventValueProviderExpressionResolver
{
    [GeneratedRegex("^\\$causedBy(?:\\((?<property>subject|name|userName)\\))?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    static partial Regex CausedByRegEx { get; }

    /// <inheritdoc/>
    public bool CanResolve(string expression) => CausedByRegEx.IsMatch(expression);

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var property = CausedByRegEx.Match(expression).Groups["property"].Value;
        return EventValueProviders.EventContext(property.Length == 0 ? "causedBy" : $"causedBy.{property}");
    }
}
