// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events;

/// <summary>
/// The exception that is thrown when an observer combines <see cref="EventStoreAttribute"/> with an explicit event sequence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStoreCannotBeCombinedWithExplicitEventSequence"/> class.
/// </remarks>
/// <param name="observerType">The observer type.</param>
/// <param name="eventStore">The event store declared on the observer.</param>
/// <param name="eventSequenceId">The explicit event sequence id.</param>
public class EventStoreCannotBeCombinedWithExplicitEventSequence(Type observerType, string eventStore, EventSequenceId eventSequenceId)
    : Exception($"Observer '{observerType.FullName}' combines [EventStore(\"{eventStore}\")] with explicit event sequence '{eventSequenceId}'. Remove the explicit event sequence configuration to use EventStore-based inbox routing.");
