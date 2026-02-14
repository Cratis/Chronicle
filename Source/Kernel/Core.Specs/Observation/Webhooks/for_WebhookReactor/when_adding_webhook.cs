// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_adding_webhook : given.a_webhook_reactor
{
    WebhookAdded _event;
    EventContext _eventContext;

    void Establish()
    {
        var webhookId = new WebhookId(Guid.NewGuid().ToString());
        const WebhookOwner owner = WebhookOwner.Client;
        var eventSequenceId = new EventSequenceId(Guid.NewGuid().ToString());
        var eventTypes = new[] { new EventType("TestEvent", 1) };
        var targetUrl = new WebhookTargetUrl("https://example.com/webhook");
        var targetHeaders = new Dictionary<string, string>();

        _event = new WebhookAdded(
            owner,
            eventSequenceId,
            eventTypes,
            targetUrl,
            targetHeaders,
            true,
            true);

        _eventContext = EventContext.From(
            new EventStoreName("test-store"),
            EventStoreNamespaceName.Default,
            new EventType("TestEvent", 1),
            EventSourceType.Default,
            new EventSourceId(webhookId.Value),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            new CorrelationId(Guid.NewGuid()));
    }

    async Task Because() => await _reactor.Added(_event, _eventContext);

    [Fact] void should_add_webhook_definition_to_manager() =>
        _webhooksManager.Received(1).Add(Arg.Is<WebhookDefinition>(def =>
            def.Identifier == new WebhookId(_eventContext.EventSourceId.Value) &&
            def.Owner == _event.Owner &&
            def.EventSequenceId == _event.EventSequenceId &&
            def.EventTypes.SequenceEqual(_event.EventTypes) &&
            def.Target.Url == _event.TargetUrl &&
            def.Target.Headers.SequenceEqual(_event.TargetHeaders) &&
            def.IsReplayable == _event.IsReplayable &&
            def.IsActive == _event.IsActive));
}
