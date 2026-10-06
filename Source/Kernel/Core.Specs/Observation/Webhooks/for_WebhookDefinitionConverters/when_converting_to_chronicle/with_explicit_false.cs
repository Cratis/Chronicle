// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters.when_converting_to_chronicle;

public class with_explicit_false : Specification
{
    WebhookDefinition _contract;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish()
    {
        using var payload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x22, 0x03, 0x0a, 0x01, 0x75, 0x3a, 0x00, 0x42, 0x00]);
        _contract = Serializer.Deserialize<WebhookDefinition>(payload);
    }

    void Because() => _result = Serializer.DeepClone(_contract).ToChronicle();

    [Fact] void should_keep_the_legacy_replayability_default() => _contract.IsReplayable.ShouldBeTrue();
    [Fact] void should_keep_the_legacy_activity_default() => _contract.IsActive.ShouldBeTrue();
    [Fact] void should_use_explicit_false_for_replayability() => _result.IsReplayable.ShouldBeFalse();
    [Fact] void should_use_explicit_false_for_activity() => _result.IsActive.ShouldBeFalse();
}
