// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A <c>[ReadModel]</c> type already becomes its own <c>&lt;Name&gt;Response</c> message through the per-service
/// DTO path - the registry must not also try to mirror it as a shared type, or the same read model would be
/// generated twice, disagreeing with itself. Declared under a Chronicle namespace deliberately, so this is the
/// read-model exclusion being exercised and not the namespace check rejecting it for an unrelated reason.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_a_read_model : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedReadModel));

    [Fact] void should_be_null() => _qualifiedName.ShouldBeNull();
}
