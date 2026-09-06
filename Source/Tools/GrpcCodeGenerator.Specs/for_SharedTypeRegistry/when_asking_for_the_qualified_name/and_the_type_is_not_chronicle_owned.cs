// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A type outside the Chronicle namespace tree entirely - a BCL type, or a third-party library type that slipped
/// through the earlier primitive/collection checks - is never a candidate. The registry has no business inventing
/// a mirror for something it does not own.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_not_chronicle_owned : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(Uri));

    [Fact] void should_be_null() => _qualifiedName.ShouldBeNull();
}
