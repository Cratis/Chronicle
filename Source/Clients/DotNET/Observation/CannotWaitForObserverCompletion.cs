// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when observer completion is waited for on an append result that carries no observer surface.
/// </summary>
/// <remarks>
/// Waiting is only meaningful against a connection that actually has observers behind it. The in-process testing
/// surfaces (<c>EventScenario</c>, <c>EventStoreForTesting</c>) run the event sequence without a silo, so no
/// projection, reducer or reactor ever runs from an append there and there is nothing whose completion could be
/// observed. Reporting success would let a spec assert that downstream work finished when none of it was ever
/// started, so the wait fails by name instead - assert on the scenario's own surface, or move the spec to an
/// out-of-process integration spec where observers genuinely run.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> the append targeted.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the append targeted.</param>
public class CannotWaitForObserverCompletion(EventStoreName eventStore, EventSequenceId eventSequenceId)
    : Exception($"Cannot wait for observer completion of event sequence '{eventSequenceId}' in event store '{eventStore}' - the append result carries no observer surface. In-process test scenarios run no observers, so completion cannot be determined.");
