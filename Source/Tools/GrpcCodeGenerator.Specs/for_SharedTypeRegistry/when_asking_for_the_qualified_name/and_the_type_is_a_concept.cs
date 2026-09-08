// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A <c>ConceptAs&lt;T&gt;</c> already travels as its unwrapped primitive - it must never also be treated as a
/// type to mirror in its own right, or the wire would carry a message for something that is really just a string.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_is_a_concept : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedConcept));

    [Fact] void should_be_null() => _qualifiedName.ShouldBeNull();
}
