// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_an_older_client_registers_on_another_sequence : given.an_observer_with_a_versioned_owner_on_another_sequence
{
    Exception _exception;

    void Establish() => _client = _client with { Version = "0.9.0" };
    async Task Because() => _exception = await Catch.Exception(Register);

    [Fact] void should_defer_to_the_newer_owner() => _exception.ShouldBeOfExactType<ObserverOwnedByNewerClientVersion>();
    [Fact] void should_keep_the_original_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_definitionEventSequenceId);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_claim_ownership() => await _eventStoreNamespaceStorage.Observers.DidNotReceive().ClaimOwnership(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>());
    [Fact] void should_leave_the_recommendation_to_the_standby() => ShouldNotRecommend();
}
