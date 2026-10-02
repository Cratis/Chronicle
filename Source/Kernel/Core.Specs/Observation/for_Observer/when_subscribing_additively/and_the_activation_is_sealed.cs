// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_additively;

public class and_the_activation_is_sealed : given.a_sealed_observer
{
    async Task Because() => await _observer.SubscribeAdditively<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_subscribe() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_not_request_recovery() => (await _observer.NeedsSubscriptionRecovery([EventType.Unknown])).ShouldBeFalse();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _silo.StorageManager.GetStorageStats(nameof(ObserverDefinition))!.Writes.ShouldEqual(0);
    [Fact] void should_not_read_schemas() => _eventTypesStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_subscribe_the_queue() => _appendedEventsQueues.ReceivedCalls().ShouldBeEmpty();
}
