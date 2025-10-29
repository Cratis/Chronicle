// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionBuilder;

public class when_building_webhook_with_builder : Specification
{
    WebhookId _webhookId;
    WebhookTargetUrl _targetUrl;
    IEventTypes _eventTypes;
    WebhookDefinitionBuilder _builder;
    WebhookDefinition _result;

    void Establish()
    {
        _webhookId = "some-id";
        _targetUrl = new WebhookTargetUrl("https://example.test/webhook");
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.All.Returns([]);
        _builder = new WebhookDefinitionBuilder(_eventTypes);
    }

    void Because()
    {
        _result = _builder.Build(_webhookId, _targetUrl);
    }

    [Fact]
    void should_set_identifier() => _result.Identifier.ShouldEqual(_webhookId);

    [Fact]
    void should_set_target_url() => _result.Target.Url.ShouldEqual(_targetUrl);

    [Fact]
    void should_set_event_sequence() => _result.EventSequenceId.ShouldEqual(EventSequenceId.Log);

    [Fact]
    void should_mark_as_replayable() => _result.IsReplayable.ShouldBeTrue();

    [Fact]
    void should_be_active_by_default() => _result.IsActive.ShouldBeTrue();

    [Fact]
    void should_have_no_event_types() => _result.EventTypes.ShouldBeEmpty();
}
