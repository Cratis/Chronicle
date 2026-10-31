// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class and_the_kernel_lacks_precise_schemas : given.an_event_target<and_the_kernel_lacks_precise_schemas.AmountRecorded>
{
    const string Legacy = """{"type":"object","properties":{"amount":{"default":null}}}""";

    void Establish()
    {
        _schemaGenerator.GenerateLegacyEventType(typeof(AmountRecorded)).Returns(JsonSchema.FromJson(Legacy));
        _schemaGenerator.ClearReceivedCalls();
    }

    Task Because() => Exercise();

    [Fact] void should_register_the_unchanged_legacy_sink_schema() => _request.ReadModels.Single().Schema.ShouldEqual(Legacy);
    [Fact] void should_not_generate_a_precise_event_sink_schema() => _schemaGenerator.DidNotReceive().Generate(typeof(AmountRecorded));

    [EventType, Public]
    public record AmountRecorded(decimal? Amount = null);
}
