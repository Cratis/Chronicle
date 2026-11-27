// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_failed_target_has_a_superseded_registration : given.an_observer_that_lost_its_client
{
    ConnectedClient _replacement;
    Exception? _setupError;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _replacement = new ConnectedClient { ConnectionId = "replacement", Version = "2.0.0" };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromException<IImmutableList<JobState>>(new TimeoutException("Job lookup failed")));
        var otherType = new EventType("another-event-type", EventTypeGeneration.First);
        _setupError = await Catch.Exception(() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [otherType], SiloAddress.Zero, _replacement));
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);

    [Fact] void should_have_failed_setup_for_the_replacement() => _setupError.ShouldBeOfExactType<TimeoutException>();
    [Fact] async Task should_still_know_the_superseded_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeTrue();
    [Fact] async Task should_not_promote_it_to_a_target() => (await _observer.IsClientTarget(_client.ConnectionId)).ShouldBeFalse();
    [Fact] async Task should_keep_the_replacement_target() => (await _observer.IsClientTarget(_replacement.ConnectionId)).ShouldBeTrue();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
