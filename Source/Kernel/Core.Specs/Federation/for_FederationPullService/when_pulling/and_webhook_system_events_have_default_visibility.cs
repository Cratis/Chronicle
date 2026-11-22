// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_webhook_system_events_have_default_visibility : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("WebhookAdded", EventTypeVisibility.Unspecified);
        RegisterEventType("TargetHeadersSetForWebhook", EventTypeVisibility.Unspecified);
        _events.AddRange(header_events.All());
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_withhold_system_events_from_the_public_outbox() => _batch.Events.ShouldBeEmpty();
    [Fact] void should_not_emit_any_content_variants() => header_events.ContainsSentinel(JsonSerializer.Serialize(_batch)).ShouldBeFalse();
    [Fact] void should_not_serialize_event_content() => _converter.DidNotReceive().ToJsonObject(Arg.Any<System.Dynamic.ExpandoObject>(), Arg.Any<Schemas.JsonSchema>());
}
