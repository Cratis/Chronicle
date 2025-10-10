// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionConverter;

public class when_building_webhook_definition_contract : Specification
{
    WebhookDefinition _definition;
    Contracts.Observation.Webhooks.WebhookDefinition _contract;
    WebhookTarget target;
    EventType eventType;
    WebhookId id;

    void Establish()
    {
        id = new WebhookId("my-webhook");
        eventType = new EventType("EventType", EventTypeGeneration.First);
        target = new WebhookTarget(
            new WebhookTargetUrl("https://example.test/webhook"),
            AuthenticationType.None,
            null,
            null,
            null,
            new Dictionary<string, string>());

        _definition = new WebhookDefinition(
            Identifier: id,
            EventTypes: [eventType],
            Target: target,
            EventSequenceId: EventSequenceId.Log,
            IsReplayable: true,
            IsActive: true);
    }

    void Because()
    {
        _contract = _definition.ToContract();
    }

    [Fact]
    void should_map_identifier() => _contract.Identifier.ShouldEqual(id.Value);

    [Fact]
    void should_map_event_sequence() => _contract.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);

    [Fact]
    void should_map_event_types()
    {
        _contract.EventTypes.ShouldContainSingleItem();
        _contract.EventTypes[0].Id.ShouldEqual(eventType.Id.Value);
    }

    [Fact]
    void should_map_target_url() => _contract.Target.Url.ShouldEqual(target.Url.Value);

    [Fact]
    void should_map_authentication_type() => _contract.Target.Authentication.ShouldEqual(
        Contracts.Observation.Webhooks.AuthenticationType.None);
}
