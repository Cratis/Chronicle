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
    WebhookTarget _target;
    EventType _eventType;
    WebhookId _id;

    void Establish()
    {
        _id = new WebhookId("my-webhook");
        _eventType = new EventType("EventType", EventTypeGeneration.First);
        _target = new WebhookTarget(
            new WebhookTargetUrl("https://example.test/webhook"),
            default(OneOf.Types.None),
            new Dictionary<string, string>());

        _definition = new WebhookDefinition(
            Identifier: _id,
            EventTypes: [_eventType],
            Target: _target,
            EventSequenceId: EventSequenceId.Log,
            IsReplayable: true,
            IsActive: true);
    }

    void Because()
    {
        _contract = _definition.ToContract();
    }

    [Fact]
    void should_map_identifier() => _contract.Identifier.ShouldEqual(_id.Value);

    [Fact]
    void should_map_event_sequence() => _contract.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);

    [Fact]
    void should_map_event_types()
    {
        _contract.EventTypes.ShouldContainSingleItem();
        _contract.EventTypes[0].Id.ShouldEqual(_eventType.Id.Value);
    }

    [Fact]
    void should_map_target_url() => _contract.Target.Url.ShouldEqual(_target.Url.Value);

    [Fact]
    void should_not_have_authorization() => _contract.Target.Authorization.ShouldBeNull();
}
