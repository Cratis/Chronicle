// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Decisions;

/// <summary>
/// Reasons why the event log cannot provide an exact concurrency scope for a read model.
/// </summary>
public enum DecisionReadRefusal
{
    /// <summary>The read can be guarded.</summary>
    None = 0,

    /// <summary>The read model or its projection has no known definition.</summary>
    UnknownDefinition = 1,

    /// <summary>The read model belongs to a reducer.</summary>
    Reducer = 2,

    /// <summary>The projection contains a join.</summary>
    Join = 3,

    /// <summary>Some projected event is keyed by something other than its event source.</summary>
    NotEventSourceKeyed = 4,

    /// <summary>The projection has child or nested hierarchies.</summary>
    Hierarchy = 5,

    /// <summary>The projection subscribes to all current and future event types.</summary>
    OpenEndedEventTypes = 6,

    /// <summary>The key does not designate one event source.</summary>
    UnspecifiedKey = 7,

    /// <summary>The projection uses a sequence other than the event log.</summary>
    NotEventLog = 8,

    /// <summary>The projection definition changed while the read was in progress.</summary>
    DefinitionChanged = 9
}
