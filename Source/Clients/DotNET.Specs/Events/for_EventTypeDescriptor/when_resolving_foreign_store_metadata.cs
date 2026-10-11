// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.for_EventTypeDescriptor;

public class when_resolving_foreign_store_metadata : Specification
{
    EventTypeDescriptor _typeDeclaration;
    EventTypeDescriptor _assemblyDeclaration;

    void Because()
    {
        _typeDeclaration = EventTypeDescriptor.FromDeclaration(typeof(Foreign), "current");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ForeignStore_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(EventStoreAttribute).GetConstructor([typeof(string)])!, ["other"]));
        var type = assembly.DefineDynamicModule("Main").DefineType("ForeignEvent", TypeAttributes.Public);
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(EventTypeAttribute).GetConstructor([typeof(string), typeof(uint)])!, ["foreign-assembly-event", 1U]));
        _assemblyDeclaration = EventTypeDescriptor.FromDeclaration(type.CreateType()!, "current");
    }

    [Fact] void should_register_type_owned_events_as_public() => _typeDeclaration.RegistrationVisibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_not_publish_foreign_type_owned_events() => _typeDeclaration.PublicationEligibility.ShouldBeFalse();
    [Fact] void should_register_assembly_owned_events_as_public() => _assemblyDeclaration.RegistrationVisibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_not_publish_foreign_assembly_owned_events() => _assemblyDeclaration.PublicationEligibility.ShouldBeFalse();
    [Fact] void should_keep_the_default_publishing_sequence() => _assemblyDeclaration.PublishingSequence.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_preserve_the_owning_store() => _typeDeclaration.EventStore.ShouldEqual("other");

    [EventType]
    [EventStore("other")]
    public record Foreign;
}
