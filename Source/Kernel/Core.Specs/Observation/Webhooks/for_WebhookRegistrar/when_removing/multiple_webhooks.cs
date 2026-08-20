// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_removing;

/// <summary>
/// Each removal targets its own event source, so the appends are issued together instead of paying a round trip per
/// webhook.
/// </summary>
public class multiple_webhooks : given.a_webhook_registrar
{
    const int NumberOfWebhooks = 3;

    readonly ConcurrentCallGate _gate = new(NumberOfWebhooks);
    IEventSequence _systemEventSequence;
    IEnumerable<string> _webhooks;

    void Establish()
    {
        _systemEventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemEventSequence);
        _webhooks = [.. Enumerable.Range(0, NumberOfWebhooks).Select(index => $"webhook-{index}")];
        _systemEventSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_ => AppendWhenAllAreInFlight());
    }

    Task Because() => _registrar.Remove("some-event-store", _webhooks);

    [Fact] void should_issue_every_append_at_the_same_time() => _gate.AllCallsWereConcurrent.ShouldBeTrue();
    [Fact] void should_append_a_removal_for_every_webhook() =>
        _systemEventSequence.Received(NumberOfWebhooks).Append(
            Arg.Any<EventSourceId>(),
            Arg.Is<object>(@event => @event is WebhookRemoved));

    [Fact]
    void should_target_each_webhook_with_its_own_event_source()
    {
        foreach (var webhookId in _webhooks)
        {
            _systemEventSequence.Received(1).Append(
                (EventSourceId)webhookId,
                Arg.Is<object>(@event => @event is WebhookRemoved));
        }
    }

    async Task<AppendResult> AppendWhenAllAreInFlight()
    {
        await _gate.Enter();
        return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
    }
}
