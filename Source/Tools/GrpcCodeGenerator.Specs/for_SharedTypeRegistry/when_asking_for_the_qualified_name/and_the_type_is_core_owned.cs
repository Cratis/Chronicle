// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A type Core declares directly under a Chronicle area namespace - <c>JobStatus</c> under
/// <c>Cratis.Chronicle.Jobs</c> is the real shape - has nowhere else to come from, so it is exactly the case the
/// registry exists to mirror.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_core_owned : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedStatus));

    [Fact] void should_not_be_null() => _qualifiedName.ShouldNotBeNull();
    [Fact] void should_map_into_the_base_namespace_under_its_own_area() =>
        _qualifiedName.ShouldEqual("global::Cratis.Chronicle.Contracts.SharedTypeCatalog.CoreOwnedStatus");
    [Fact] void should_register_it_as_discovered() =>
        SharedTypeRegistry.Discovered.ContainsKey(typeof(CoreOwnedStatus)).ShouldBeTrue();
}
