// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookDefinitionConverter;

public class when_round_tripping_non_replayable_inactive_webhook : Specification
{
    WebhookDefinition _definition;
    Contracts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new(
        "webhook",
        [],
        new("https://example.test", default(OneOf.Types.None), new Dictionary<string, string>()),
        EventSequenceId.Log,
        false,
        false);

    void Because()
    {
        using var payload = new MemoryStream();
        Serializer.Serialize(payload, _definition.ToContract());
        payload.Position = 0;
        _result = Serializer.Deserialize<Contracts.Observation.Webhooks.WebhookDefinition>(payload);
    }

    [Fact] void should_send_legacy_false_replayability() => _result.IsReplayable.ShouldBeFalse();
    [Fact] void should_send_legacy_false_activity() => _result.IsActive.ShouldBeFalse();
    [Fact] void should_send_explicit_false_replayability() => _result.IsReplayableValue.ShouldEqual(BooleanValue.False);
    [Fact] void should_send_explicit_false_activity() => _result.IsActiveValue.ShouldEqual(BooleanValue.False);
}
