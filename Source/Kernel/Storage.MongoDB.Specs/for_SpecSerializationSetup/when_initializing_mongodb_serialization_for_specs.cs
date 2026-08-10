// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Serialization;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.for_SpecSerializationSetup;

public class when_initializing_mongodb_serialization_for_specs : Specification
{
    MongoDBBuilder _builder = default!;

    void Establish() => _builder = new MongoDBBuilder();

    [Fact] void should_use_generated_type_discovery() => global::Cratis.Types.Types.Instance.DiscoveryMode.ShouldEqual(TypeDiscoveryMode.Generated);
    [Fact] void should_include_the_spec_assembly_in_type_discovery() => global::Cratis.Types.Types.Instance.Assemblies.ShouldContain(typeof(SpecSerializationSetup).Assembly);
    [Fact] void should_discover_the_event_class_map() => _builder.ClassMaps.ShouldContain(typeof(EventClassMap));
    [Fact] void should_discover_the_constraint_class_map() => _builder.ClassMaps.ShouldContain(typeof(UniqueConstraintDefinitionClassMap));
    [Fact] void should_discover_the_chronicle_convention_pack_provider() => _builder.ConventionPackProviders.ShouldContain(typeof(ConventionPacks));
    [Fact] void should_discover_the_arc_convention_pack_filter() => _builder.ConventionPackFilters.ShouldContain(typeof(IgnoreConventionsAttributeFilter));
    [Fact] void should_discover_derived_types() => DerivedTypes.Instance.HasDerivatives(typeof(ISpecSerializationTarget)).ShouldBeTrue();
    [Fact] void should_register_the_derived_type_discriminator_convention() =>
        BsonSerializer.LookupDiscriminatorConvention(typeof(ISpecSerializationTarget)).ShouldBeOfExactType<DerivedTypeDiscriminatorConvention>();
}
