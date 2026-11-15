// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the kind of event sequence mutation.
/// </summary>
public enum EventSequenceMutationKind
{
    /// <summary>
    /// The mutation kind is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The mutation revises events.
    /// </summary>
    Revision = 1,

    /// <summary>
    /// The mutation redacts a specific event.
    /// </summary>
    PointRedaction = 2,

    /// <summary>
    /// The mutation redacts events for an event source.
    /// </summary>
    EventSourceRedaction = 3,

    /// <summary>
    /// The mutation backfills event type generations.
    /// </summary>
    GenerationBackfill = 4
}
