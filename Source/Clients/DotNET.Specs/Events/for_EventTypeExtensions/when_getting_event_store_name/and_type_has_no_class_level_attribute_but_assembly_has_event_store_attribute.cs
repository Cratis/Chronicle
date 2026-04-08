// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_store_name;

public class and_type_has_no_class_level_attribute_but_assembly_has_event_store_attribute : Specification
{
    const string SourceEventStore = "assembly-level-event-store";

    Type _typeFromAssemblyWithAttribute = null!;
    string? _result;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TestAssemblyWithEventStoreAttribute"),
            AssemblyBuilderAccess.Run);

        var eventStoreAttributeConstructor = typeof(EventStoreAttribute).GetConstructor([typeof(string)])!;
        assembly.SetCustomAttribute(
            new CustomAttributeBuilder(eventStoreAttributeConstructor, [SourceEventStore]));

        var module = assembly.DefineDynamicModule("TestModule");
        _typeFromAssemblyWithAttribute = module.DefineType("EventFromAssemblyWithAttribute").CreateType()!;
    }

    void Because() => _result = _typeFromAssemblyWithAttribute.GetEventStoreName();

    [Fact] void should_return_the_assembly_level_event_store_name() => _result.ShouldEqual(SourceEventStore);
}
