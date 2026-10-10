// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_a_reconnected_kernel_advertises_precise_schemas : given.all_dependencies
{
    [EventType]
    record AmountRecorded(decimal Amount);
    EventTypes _subject;
    RegisterEventTypesRequest _request;

    async Task Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(AmountRecorded)]);
        _schemaGenerator.Generate(typeof(AmountRecorded)).Returns(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null}}}"""));
        _schemaGenerator.GenerateLegacyEventType(typeof(AmountRecorded)).Returns(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"amount":{"default":null}}}"""));
        ((IKernelCapabilities)_eventStore.Connection).Capabilities.Returns([]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
        await _subject.Discover();
        await _subject.Register();
        ((IKernelCapabilities)_eventStore.Connection).Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        _ = _eventTypesService.RegisterEventTypes(Arg.Do<RegisterEventTypesRequest>(request => _request = request));
    }

    Task Because() => _subject.Register();

    [Fact] void should_upgrade_the_registered_shape() => _request.Types.Single().Schema.ShouldEqual("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null}}}""");
}
