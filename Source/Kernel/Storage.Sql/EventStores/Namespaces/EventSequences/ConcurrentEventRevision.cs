// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Exception raised when concurrent writes prevent a revision from being persisted.
/// </summary>
/// <param name="sequenceNumber">The event sequence number.</param>
public sealed class ConcurrentEventRevision(EventSequenceNumber sequenceNumber)
    : Exception($"Event with sequence number {sequenceNumber} was modified concurrently while revising it");
