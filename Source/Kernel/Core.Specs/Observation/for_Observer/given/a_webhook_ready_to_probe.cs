// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_webhook_ready_to_probe : a_webhook_observer
{
    protected IWebhookObserverSubscriber _webhook;
    protected IEventCursor _cursor;
    protected IWebhookDefinitionsStorage _webhookDefinitions;
    protected WebhookDefinition _webhookDefinition;
    protected AppendedEvent _event;
    protected DateTimeOffset _now;

    async Task Establish()
    {
        _observer.ProbeRandom = () => 0.5;
        _webhook = Substitute.For<IWebhookObserverSubscriber>();
        _subscriber = _webhook;
        _silo.AddProbe<IWebhookObserverSubscriber>(_ => _webhook);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(call => call.Arg<IEnumerable<AppendedEvent>>().ToArray());
        _event = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber("Event1", 42UL);
        _cursor = Substitute.For<IEventCursor>();
        _cursor.MoveNext().Returns(true);
        _cursor.Current.Returns([_event]);
        _eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, 32).ReturnsForAnyArgs(_cursor);
        _webhookDefinitions = Substitute.For<IWebhookDefinitionsStorage>();
        _webhookDefinition = new(new(_observerId.Value), WebhookOwner.Kernel, EventSequenceId.Log, ["Event1", "Event2"], new("https://receiver.example/hook", WebhookAuthorization.None, new Dictionary<string, string>()), true);
        _webhookDefinitions.Get(_observerId).Returns(_webhookDefinition);
        _eventStoreStorage.Webhooks.Returns(_webhookDefinitions);
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(ObserverSubscriberResult.Ok(42UL));
        await _observer.PartitionFailed("partition", 42UL, ["403"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.PauseObserver);
        _now = _stateStorage.State.WebhookProbe.Due;
        _observer.ProbeTimeProvider = new ProbeClock(() => _now);
        _webhook.ClearReceivedCalls();
    }

    protected Task Tick() => _observer.ReceiveReminder(Observer.WebhookProbeReminderName, default);

    sealed class ProbeClock(Func<DateTimeOffset> now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now();
    }
}
