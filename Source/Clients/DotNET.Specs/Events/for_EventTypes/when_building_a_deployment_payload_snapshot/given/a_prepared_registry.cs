// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_building_a_deployment_payload_snapshot.given;

public class a_prepared_registry : for_EventTypes.given.all_dependencies
{
    protected IKernelCapabilities _kernelCapabilities;
    protected EventTypes _registry;
    protected JsonSchema _liveSchema;
    protected RegisterEventTypesRequest _ordinary;
    protected RegisterEventTypesRequest _snapshot;
    protected bool _fail;

    async Task Establish()
    {
        _kernelCapabilities = (IKernelCapabilities)_eventStore.Connection;
        _kernelCapabilities.CapabilitiesAreKnown.Returns(true);
        _kernelCapabilities.Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        _clientArtifacts.EventTypes.Returns([typeof(AnEvent)]);
        _schemaGenerator.Generate(Arg.Any<Type>(), Arg.Any<IEventTypes>()).Returns(call =>
            _fail
                ? throw new InvalidOperationException("diagnostic schema failure")
                : new JsonSchema());
        _registry = new(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
        await _registry.Discover();
        _ordinary = await _registry.PrepareRegistration();
        _liveSchema = _registry.GetSchemaFor(_registry.GetEventTypeFor(typeof(AnEvent)).Id);
    }

    [EventType("c3b27ed8-fb48-493f-8e50-5cc71f9467ba")]
    public record AnEvent(string Name);
}
