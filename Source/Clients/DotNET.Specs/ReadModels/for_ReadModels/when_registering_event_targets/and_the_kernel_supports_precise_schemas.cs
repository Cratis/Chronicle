// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class and_the_kernel_supports_precise_schemas : given.an_event_target<and_the_kernel_supports_precise_schemas.AmountRecorded>
{
    const string Precise = """{"type":"object","properties":{"amount":{"default":null,"type":"number","format":"decimal?"}}}""";

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IKernelCapabilities>();
        ((IKernelCapabilities)connection).Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        _eventStore.Connection.Returns(connection);
        _schemaGenerator.Generate(typeof(AmountRecorded)).Returns(JsonSchema.FromJson(Precise));
        _schemaGenerator.ClearReceivedCalls();
    }

    Task Because() => Exercise();

    [Fact] void should_register_the_precise_sink_schema() => _request.ReadModels.Single().Schema.ShouldEqual(Precise);
    [Fact] void should_not_generate_a_legacy_event_sink_schema() => _schemaGenerator.DidNotReceive().GenerateLegacyEventType(typeof(AmountRecorded));

    [EventType, Public]
    public record AmountRecorded(decimal? Amount = null);
}
