// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Exception that gets thrown when an event could not be revised because other writers kept revising it concurrently.
/// </summary>
/// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event.</param>
public class ConcurrentEventRevision(EventSequenceNumber sequenceNumber) : Exception($"The event at sequence number {sequenceNumber} could not be revised because it was changed concurrently too many times.");
