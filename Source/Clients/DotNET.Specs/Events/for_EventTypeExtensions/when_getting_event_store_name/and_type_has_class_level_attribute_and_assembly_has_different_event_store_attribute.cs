// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_store_name;

public class and_type_has_class_level_attribute_and_assembly_has_different_event_store_attribute : Specification
{
    const string ClassLevelEventStore = "class-level-event-store";
    const string AssemblyLevelEventStore = "assembly-level-event-store";

    Type _typeFromAssemblyWithAttribute = null!;
    string? _result;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TestAssemblyWithDifferentEventStoreAttribute"),
            AssemblyBuilderAccess.Run);

        var eventStoreAttributeConstructor = typeof(EventStoreAttribute).GetConstructor([typeof(string)])!;
        assembly.SetCustomAttribute(
            new CustomAttributeBuilder(eventStoreAttributeConstructor, [AssemblyLevelEventStore]));

        var module = assembly.DefineDynamicModule("TestModule");
        var typeBuilder = module.DefineType("EventWithClassLevelOverride");
        typeBuilder.SetCustomAttribute(
            new CustomAttributeBuilder(eventStoreAttributeConstructor, [ClassLevelEventStore]));
        _typeFromAssemblyWithAttribute = typeBuilder.CreateType()!;
    }

    void Because() => _result = _typeFromAssemblyWithAttribute.GetEventStoreName();

    [Fact] void should_return_the_class_level_event_store_name() => _result.ShouldEqual(ClassLevelEventStore);
}
