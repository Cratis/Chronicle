// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A type that already lives under the contracts namespace is never a candidate - mirroring it would try to
/// generate a file describing itself.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_already_a_contract : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(AlreadyAContractType));

    [Fact] void should_be_null() => _qualifiedName.ShouldBeNull();
    [Fact] void should_not_register_it_as_discovered() =>
        SharedTypeRegistry.Discovered.ContainsKey(typeof(AlreadyAContractType)).ShouldBeFalse();
}
