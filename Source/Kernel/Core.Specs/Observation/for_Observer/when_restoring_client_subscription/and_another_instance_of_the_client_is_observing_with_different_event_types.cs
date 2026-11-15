// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// Different event types cannot be fanned out into the existing target, so the full reconcile runs.
/// </summary>
public class and_another_instance_of_the_client_is_observing_with_different_event_types : given.an_observer_restoring_another_client_instance
{
    static readonly EventType _otherEventType = new("87e51e33-1e9c-4c66-a4a6-3a1372c7ecd6", EventTypeGeneration.First);

    Task Because() => RestoreSecondClient(eventTypes: [_otherEventType]);

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
