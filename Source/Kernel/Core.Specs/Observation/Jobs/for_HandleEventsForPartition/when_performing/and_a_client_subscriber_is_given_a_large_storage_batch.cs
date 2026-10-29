// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// A storage batch can hold thousands of events. A client subscriber gets them a bounded call at a time, so the
/// subscriber timeout guards a known amount of work.
/// </summary>
public class and_a_client_subscriber_is_given_a_large_storage_batch : given.a_performing_job_step
{
    readonly List<int> _callSizes = [];
    IReactorObserverSubscriber _client;
    Catch<JobStepResult> _result;

    void Establish()
    {
        _observersConfig = new Observers { MaxEventsPerSubscriberCall = 100 };
        var silo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        var target = new ObserverSubscriberTarget(silo, new ConnectedClient { ConnectionId = "A" });
        var subscription = new ObserverSubscription(_performState.ObserverKey.ObserverId, _performState.ObserverKey, [], typeof(IReactorObserverSubscriber), silo) { Targets = [target] };
        _observer.GetSubscription().Returns(subscription);
        _client = Substitute.For<IReactorObserverSubscriber>();
        _client.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _callSizes.Add(events.Length);
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
        _client.Drain().Returns(Task.CompletedTask);
        _silo.AddProbe(_ => _client);
        _jobStep.SetupForTesting(_observer, _client, "some-partition", subscription);
        _eventCursor.Current.Returns(
            Enumerable.Range(0, 250).Select(offset => AppendedEvent.EmptyWithEventSequenceNumber(first_event_sequence_number + (ulong)offset)).ToArray());
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_hand_the_events_over_in_bounded_calls() => _callSizes.ShouldContainOnly(100, 100, 50);

    [Fact] void should_complete_the_step() => _result.TryGetResult(out _).ShouldBeTrue();

    [Fact]
    void should_not_report_the_partition_as_failed() =>
        _observer.DidNotReceive().PartitionFailed(
            Arg.Any<Key>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            Arg.Any<Concepts.Observation.FailureKind>());
}
