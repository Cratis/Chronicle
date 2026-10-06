// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters.when_converting_to_chronicle;

public class with_explicit_true : Specification
{
    WebhookDefinition _contract;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish()
    {
        using var payload = new MemoryStream([0x0a, 0x01, 0x79, 0x12, 0x01, 0x78, 0x22, 0x03, 0x0a, 0x01, 0x75, 0x28, 0x00, 0x30, 0x00, 0x38, 0x01, 0x40, 0x01]);
        _contract = Serializer.Deserialize<WebhookDefinition>(payload);
    }

    void Because() => _result = _contract.ToChronicle();

    [Fact] void should_use_explicit_true_for_replayability() => _result.IsReplayable.ShouldBeTrue();
    [Fact] void should_use_explicit_true_for_activity() => _result.IsActive.ShouldBeTrue();
}
