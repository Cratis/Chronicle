// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_the_assembly_names_an_event_store : given.all_dependencies
{
    const string OwningEventStore = "contracts-owner";

    EventTypes _subject;
    RegisterEventTypesRequest _capturedRequest;

    void Establish()
    {
        // The contracts convention: the assembly is marked with the owning event store, the event type itself is not.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ContractsAssemblyForVisibility"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(EventStoreAttribute).GetConstructor([typeof(string)])!, [OwningEventStore]));
        var type = assembly.DefineDynamicModule("Module").DefineType("ContractsEvent");
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(EventTypeAttribute).GetConstructor([typeof(string), typeof(uint)])!, ["contracts-event", 1u]));

        _clientArtifacts.EventTypes.Returns([type.CreateType()]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);

        _eventTypesService
            .When(_ => _.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _capturedRequest = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_register_it_as_public() => _capturedRequest.Types.ElementAt(0).Visibility.ShouldEqual(Contracts.Events.EventTypeVisibility.Public);
    [Fact] void should_register_the_assembly_event_store_as_origin() => _capturedRequest.Types.ElementAt(0).EventStore.ShouldEqual(OwningEventStore);
}
