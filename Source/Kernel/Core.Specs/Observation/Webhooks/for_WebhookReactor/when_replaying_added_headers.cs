// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_replaying_added_headers : given.a_webhook_reactor
{
    WebhookAdded _event;
    AppendedEvent _stored;
    string _public;

    void Establish()
    {
        _stored = header_events.All().First(@event => @event.Context.EventType.Id == "WebhookAdded");
        var headers = (IReadOnlyDictionary<string, string>)((IDictionary<string, object?>)_stored.Content)["targetHeaders"]!;
        _event = new(WebhookOwner.Client, "event-log", [], "https://receiver.example.com", headers, true, true);
    }

    async Task Because()
    {
        _public = Sequences.AppendedEventConverters.ToApi(_stored, header_events.Options).Content;
        await _reactor.Added(_event, _stored.Context with { EventSourceId = "webhook" });
    }

    [Fact] void should_redact_the_public_view() => header_events.HasEmptyHeaders(_public).ShouldBeTrue();

    [Fact] void should_keep_real_values_for_internal_delivery() => _webhooksManager.Received(1).Add(Arg.Is<WebhookDefinition>(definition => definition.Target.Headers["X-First"] == header_events.Sentinels[0]));
}
