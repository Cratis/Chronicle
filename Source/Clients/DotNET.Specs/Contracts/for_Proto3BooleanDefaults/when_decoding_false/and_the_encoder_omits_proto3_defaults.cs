// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_Proto3BooleanDefaults.when_decoding_false;

public class and_the_encoder_omits_proto3_defaults : Specification
{
    ReactorDefinition _reactor;
    WebhookDefinition _webhook;

    void Because()
    {
        // Proto3 omits legacy false scalars and sends true for the inverted companions.
        using var reactorPayload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x38, 0x01]);
        using var webhookPayload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x38, 0x01, 0x40, 0x01]);
        _reactor = Serializer.Deserialize<ReactorDefinition>(reactorPayload);
        _webhook = Serializer.Deserialize<WebhookDefinition>(webhookPayload);
    }

    [Fact] void should_preserve_false_for_reactor_replayability() => (_reactor.IsReplayable && !_reactor.IsNotReplayable).ShouldBeFalse();
    [Fact] void should_preserve_false_for_webhook_replayability() => (_webhook.IsReplayable && !_webhook.IsNotReplayable).ShouldBeFalse();
    [Fact] void should_preserve_false_for_webhook_activity() => (_webhook.IsActive && !_webhook.IsInactive).ShouldBeFalse();
}
