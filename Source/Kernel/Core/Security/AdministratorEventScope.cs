// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Identifies events belonging to the system administrator roster.
/// </summary>
internal static class AdministratorEventScope
{
    /// <summary>
    /// Determines whether the event belongs to the default namespace and stream of the System event store.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>True if the event belongs to the System event store; otherwise false.</returns>
    internal static bool IsSystemEvent(EventContext context) =>
        string.Equals(context.EventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase) &&
        context.Namespace == EventStoreNamespaceName.Default &&
        context.EventStreamType == EventStreamType.All &&
        context.EventStreamId == EventStreamId.Default;

    /// <summary>
    /// Determines whether the event belongs to the System administrator roster event source.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>True if the event belongs to the administrator roster; otherwise false.</returns>
    internal static bool IsRosterEvent(EventContext context) =>
        IsSystemEvent(context) && context.EventSourceId == Roster.EventSourceId;
}
