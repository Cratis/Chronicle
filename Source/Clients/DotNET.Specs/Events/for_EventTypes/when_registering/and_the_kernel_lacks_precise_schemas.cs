// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_the_kernel_lacks_precise_schemas : given.all_dependencies
{
    [EventType]
    record AmountRecorded(decimal Amount);

    EventTypes _subject;
    RegisterEventTypesRequest _request;

    void Establish()
    {
        ((IKernelCapabilities)_eventStore.Connection).Capabilities.Returns([]);
        _clientArtifacts.EventTypes.Returns([typeof(AmountRecorded)]);
        _schemaGenerator.GenerateLegacyEventType(typeof(AmountRecorded)).Returns(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}"""));
        _eventTypesService.RegisterEventTypes(Arg.Do<RegisterEventTypesRequest>(request => _request = request));
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_register_the_legacy_shape() => _request.Types.Single().Schema.ShouldEqual("""{"type":"object","properties":{"amount":{"default":null}}}""");
    [Fact] void should_use_the_legacy_generator() => _schemaGenerator.Received(1).GenerateLegacyEventType(typeof(AmountRecorded));
}
