// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot;

public class and_the_kernel_capability_changes : given.a_prepared_registry
{
    string _legacy;
    string _precise;
    bool _schemaUnchanged;
    string _next;

    async Task Establish()
    {
        _legacy = """{"type":"object","properties":{"amount":{}}}""";
        _precise = """{"type":"object","properties":{"amount":{"type":"number"}}}""";
        _schemaGenerator.GenerateLegacyEventType(typeof(AnEvent), Arg.Any<IEventTypes>()).Returns(await JsonSchema.FromJsonAsync(_legacy));
        _schemaGenerator.Generate(typeof(AnEvent), Arg.Any<IEventTypes>()).Returns(await JsonSchema.FromJsonAsync(_precise));
        _kernelCapabilities.Capabilities.Returns([]);
        _ordinary = await _registry.PrepareRegistration();
        _liveSchema = _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id);
    }

    async Task Because()
    {
        _kernelCapabilities.Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        _snapshot = await _registry.BuildDeploymentPayloadSnapshot();
        _schemaUnchanged = ReferenceEquals(_liveSchema, _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id));
        _next = (await _registry.PrepareRegistration()).Types.Single().Schema;
    }

    [Fact] void should_have_prepared_legacy_bytes() => _ordinary.Types.Single().Schema.ShouldEqual(_legacy);
    [Fact] void should_build_precise_snapshot_bytes() => _snapshot.Types.Single().Schema.ShouldEqual(_precise);
    [Fact] void should_preserve_the_live_schema_instance() => _schemaUnchanged.ShouldBeTrue();
    [Fact] void should_preserve_the_live_schema_content() => _liveSchema.ToJson().ShouldEqual(_legacy);
    [Fact] void should_allow_the_next_normal_preparation_to_upgrade() => _next.ShouldEqual(_snapshot.Types.Single().Schema);
}
