// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_Proto3BooleanDefaults.when_decoding_false;

public class and_the_encoder_omits_proto3_defaults : Specification
{
    ReactorDefinition _reactor;
    WebhookDefinition _webhook;

    void Because()
    {
        // Proto3 encoders still omit the legacy false scalars. The additive enum fields
        // carry False (2) explicitly: reactor field 7, webhook fields 7 and 8.
        using var reactorPayload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x38, 0x02]);
        using var webhookPayload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x38, 0x02, 0x40, 0x02]);
        _reactor = Serializer.Deserialize<ReactorDefinition>(reactorPayload);
        _webhook = Serializer.Deserialize<WebhookDefinition>(webhookPayload);
    }

    [Fact] void should_preserve_false_for_reactor_replayability() => _reactor.IsReplayableValue.Resolve(_reactor.IsReplayable).ShouldBeFalse();
    [Fact] void should_preserve_false_for_webhook_replayability() => _webhook.IsReplayableValue.Resolve(_webhook.IsReplayable).ShouldBeFalse();
    [Fact] void should_preserve_false_for_webhook_activity() => _webhook.IsActiveValue.Resolve(_webhook.IsActive).ShouldBeFalse();
}
