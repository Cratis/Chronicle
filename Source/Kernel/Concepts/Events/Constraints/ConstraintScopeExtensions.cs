// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Extension methods for <see cref="ConstraintScope"/>.
/// </summary>
public static class ConstraintScopeExtensions
{
    /// <summary>
    /// Build a storage scope key from the constraint scope and the event metadata.
    /// </summary>
    /// <param name="scope">The <see cref="ConstraintScope"/> defining which dimensions to include.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the event.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the event.</param>
    /// <returns>A string key representing the scope dimensions, or empty if no scoping is applied.</returns>
    public static string BuildScopeKey(
        this ConstraintScope? scope,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId)
    {
        if (scope?.HasScope != true)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (scope.EventSourceType is not null && eventSourceType is not null)
        {
            parts.Add($"est:{eventSourceType.Value}");
        }

        if (scope.EventStreamType is not null && eventStreamType is not null)
        {
            parts.Add($"estt:{eventStreamType.Value}");
        }

        if (scope.EventStreamId is not null && eventStreamId is not null)
        {
            parts.Add($"esid:{eventStreamId.Value}");
        }

        return string.Join('|', parts);
    }
}
