// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Describes where the event of a write recording applied event markers was read from, and how the write is confirmed.
/// </summary>
/// <param name="EventSequence">The event sequence the event was read from.</param>
/// <param name="Immediately">Whether the write must have a confirmed outcome rather than be buffered in a bulk window.</param>
public record AppliedEventMarkerWriteOrigin(EventSequenceId EventSequence, bool Immediately);
