// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_removing;

/// <summary>
/// Each removal targets its own event source, so the appends are issued together instead of paying a round trip per
/// subscription.
/// </summary>
public class multiple_subscriptions : given.an_event_store_subscriptions_service
{
    const int NumberOfSubscriptions = 3;

    readonly ConcurrentCallGate _gate = new(NumberOfSubscriptions);
    RemoveEventStoreSubscriptions _request;

    void Establish()
    {
        _request = new RemoveEventStoreSubscriptions
        {
            TargetEventStore = "some-event-store",
            SubscriptionIds = [.. Enumerable.Range(0, NumberOfSubscriptions).Select(index => $"subscription-{index}")]
        };

        _systemEventSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ => AppendWhenAllAreInFlight());
    }

    Task Because() => _subject.Remove(_request);

    [Fact] void should_issue_every_append_at_the_same_time() => _gate.AllCallsWereConcurrent.ShouldBeTrue();
    [Fact] void should_append_a_removal_for_every_subscription() =>
        _systemEventSequence.Received(NumberOfSubscriptions).Append(
            Arg.Any<EventSourceId>(),
            Arg.Is<object>(@event => @event is EventStoreSubscriptionRemoved));

    [Fact]
    void should_target_each_subscription_with_its_own_event_source()
    {
        foreach (var subscriptionId in _request.SubscriptionIds)
        {
            _systemEventSequence.Received(1).Append(
                (EventSourceId)subscriptionId,
                Arg.Is<object>(@event => @event is EventStoreSubscriptionRemoved));
        }
    }

    async Task<AppendResult> AppendWhenAllAreInFlight()
    {
        await _gate.Enter();
        return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
    }
}
