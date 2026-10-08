// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters.when_converting_to_chronicle;

public class without_explicit_values : Specification
{
    WebhookDefinition _omitted;
    WebhookDefinition _false;
    Concepts.Observation.Webhooks.WebhookDefinition _omittedResult;
    Concepts.Observation.Webhooks.WebhookDefinition _falseResult;

    void Establish()
    {
        using var omittedPayload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x22, 0x03, 0x0a, 0x01, 0x75]);
        using var falsePayload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x22, 0x03, 0x0a, 0x01, 0x75, 0x28, 0x00, 0x30, 0x00]);
        _omitted = Serializer.Deserialize<WebhookDefinition>(omittedPayload);
        _false = Serializer.Deserialize<WebhookDefinition>(falsePayload);
    }

    void Because()
    {
        _omittedResult = _omitted.ToChronicle();
        _falseResult = _false.ToChronicle();
    }

    [Fact] void should_keep_true_for_omitted_replayability() => _omittedResult.IsReplayable.ShouldBeTrue();
    [Fact] void should_keep_true_for_omitted_activity() => _omittedResult.IsActive.ShouldBeTrue();
    [Fact] void should_keep_false_for_explicit_legacy_replayability() => _falseResult.IsReplayable.ShouldBeFalse();
    [Fact] void should_keep_false_for_explicit_legacy_activity() => _falseResult.IsActive.ShouldBeFalse();
}
