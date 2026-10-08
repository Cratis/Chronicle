// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters.when_round_tripping_to_contract;

public class with_boolean_values : Specification
{
    WebhookDefinition[] _results;

    void Because() => _results = new[] { true, false }.Select(value =>
    {
        var definition = new Concepts.Observation.Webhooks.WebhookDefinition(
            "webhook",
            Concepts.Observation.Webhooks.WebhookOwner.Client,
            Concepts.EventSequences.EventSequenceId.Log,
            [],
            new("https://example.test", Concepts.Observation.Webhooks.WebhookAuthorization.None, new Dictionary<string, string>()),
            value,
            value);
        using var payload = new MemoryStream();
        Serializer.Serialize(payload, definition.ToContract());
        payload.Position = 0;
        return Serializer.Deserialize<WebhookDefinition>(payload);
    }).ToArray();

    [Fact] void should_send_legacy_true_replayability() => _results[0].IsReplayable.ShouldBeTrue();
    [Fact] void should_send_legacy_true_activity() => _results[0].IsActive.ShouldBeTrue();
    [Fact] void should_not_disable_replayability() => _results[0].IsNotReplayable.ShouldBeFalse();
    [Fact] void should_not_disable_activity() => _results[0].IsInactive.ShouldBeFalse();
    [Fact] void should_send_legacy_false_replayability() => _results[1].IsReplayable.ShouldBeFalse();
    [Fact] void should_send_legacy_false_activity() => _results[1].IsActive.ShouldBeFalse();
    [Fact] void should_send_the_disabling_replayability_companion() => _results[1].IsNotReplayable.ShouldBeTrue();
    [Fact] void should_send_the_disabling_activity_companion() => _results[1].IsInactive.ShouldBeTrue();
}
