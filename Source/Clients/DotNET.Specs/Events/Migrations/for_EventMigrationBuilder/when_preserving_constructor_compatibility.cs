// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder;

public class when_preserving_constructor_compatibility : Specification
{
    ConstructorInfo _builderConstructor;
    ConstructorInfo _propertyBuilderConstructor;
    ConstructorInfo _eventTypesConstructor;

    void Because()
    {
        _builderConstructor = typeof(EventMigrationBuilder).GetConstructor(Type.EmptyTypes);
        _propertyBuilderConstructor = typeof(EventMigrationPropertyBuilder).GetConstructor(Type.EmptyTypes);
        _eventTypesConstructor = typeof(EventTypes).GetConstructor([typeof(IEventStore), typeof(IJsonSchemaGenerator), typeof(IClientArtifactsProvider), typeof(IEventTypeMigrators), typeof(bool)]);
    }

    [Fact] void should_preserve_the_parameterless_builder_constructor() => _builderConstructor.ShouldNotBeNull();
    [Fact] void should_preserve_the_parameterless_property_builder_constructor() => _propertyBuilderConstructor.ShouldNotBeNull();
    [Fact] void should_preserve_the_original_event_types_constructor() => _eventTypesConstructor.ShouldNotBeNull();
}
