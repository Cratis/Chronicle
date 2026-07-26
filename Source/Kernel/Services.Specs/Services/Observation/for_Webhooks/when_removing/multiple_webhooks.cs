// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Services.Observation.Webhooks.for_Webhooks.when_removing;

/// <summary>
/// Each removal targets its own event source, so the appends are issued together instead of paying a round trip per
/// webhook.
/// </summary>
public class multiple_webhooks : given.a_webhooks_service
{
    const int NumberOfWebhooks = 3;

    readonly ConcurrentCallGate _gate = new(NumberOfWebhooks);
    RemoveWebhooks _request;

    void Establish()
    {
        _request = new RemoveWebhooks
        {
            EventStore = "some-event-store",
            Webhooks = [.. Enumerable.Range(0, NumberOfWebhooks).Select(index => $"webhook-{index}")]
        };

        _systemEventSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ => AppendWhenAllAreInFlight());
    }

    Task Because() => _subject.Remove(_request);

    [Fact] void should_issue_every_append_at_the_same_time() => _gate.AllCallsWereConcurrent.ShouldBeTrue();
    [Fact] void should_append_a_removal_for_every_webhook() =>
        _systemEventSequence.Received(NumberOfWebhooks).Append(
            Arg.Any<EventSourceId>(),
            Arg.Is<object>(@event => @event is WebhookRemoved));

    async Task<AppendResult> AppendWhenAllAreInFlight()
    {
        await _gate.Enter();
        return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
    }
}
