// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_a_client_delivery_times_out : given.a_performing_job_step
{
    readonly TaskCompletionSource _barrier = new();
    IReactorObserverSubscriber _client;
    Catch<JobStepResult> _result;
    bool _held;

    void Establish()
    {
        var silo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        var target = new ObserverSubscriberTarget(silo, new ConnectedClient { ConnectionId = "A" });
        var subscription = new ObserverSubscription(_performState.ObserverKey.ObserverId, _performState.ObserverKey, [], typeof(IReactorObserverSubscriber), silo) { Targets = [target] };
        _observer.GetSubscription().Returns(subscription);
        _client = Substitute.For<IReactorObserverSubscriber>();
        _client.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(Task.FromException<ObserverSubscriberResult>(new TimeoutException()));
        _client.Drain().Returns(_barrier.Task);
        _silo.AddProbe(_ => _client);
        _jobStep.SetupForTesting(_observer, subscription);
        _eventCursor.Current.Returns([CreateEvent(1UL, "module")]);
    }

    async Task Because()
    {
        var perform = _jobStep.InvokePerformStep(_performState);
        _held = !perform.IsCompleted;
        _barrier.SetResult();
        _result = await perform;
    }

    [Fact] void should_hold_the_step_until_the_original_client_drains() => _held.ShouldBeTrue();
    [Fact] void should_not_immediately_retry_the_abandoned_delivery() => _ = _client.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact]
    void should_retain_the_original_identity_when_reporting_the_transient_timeout()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(result, new JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.DeliveryFence!.Targets!.Single().ConnectionId.ShouldEqual("A");
    }
}
