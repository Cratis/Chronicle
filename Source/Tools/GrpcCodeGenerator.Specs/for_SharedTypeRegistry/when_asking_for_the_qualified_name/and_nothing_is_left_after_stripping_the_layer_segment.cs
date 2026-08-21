// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A type declared directly under a transparent layer segment, with nothing beneath it, used to map onto a
/// dangling trailing dot ("global::Cratis.Chronicle.Contracts.RootLevelConceptsType" with an extra "." before the
/// type name) because joining an empty skipped-segment list still ran through the base-namespace-plus-dot format.
/// A real example would be a type declared straight in "Cratis.Chronicle.Concepts" rather than in an area
/// namespace beneath it.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_nothing_is_left_after_stripping_the_layer_segment : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(RootLevelConceptsType));

    [Fact] void should_mirror_straight_into_the_base_namespace() =>
        _qualifiedName.ShouldEqual("global::Cratis.Chronicle.Contracts.RootLevelConceptsType");
}
