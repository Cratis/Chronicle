// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// A type Core reuses from a project it depends on rather than declaring itself - <c>Concepts.Jobs.JobStatus</c>
/// is the real shape - sits one namespace segment deeper than a Core-declared type. Mapping it with the plain
/// skip/base transform, unadjusted, sends the file to <c>Contracts.Concepts.Jobs</c> instead of
/// <c>Contracts.Jobs</c> - a real defect caught moving <c>JobStatus</c> itself, which this guards against
/// regressing.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_comes_from_a_transparent_layer : given.a_configured_registry
{
    string? _qualifiedName;

    void Because() => _qualifiedName = SharedTypeRegistry.QualifiedNameFor(typeof(ConceptsOwnedStatus));

    [Fact] void should_strip_the_transparent_layer_segment() =>
        _qualifiedName.ShouldEqual("global::Cratis.Chronicle.Contracts.SharedTypeCatalog.ConceptsOwnedStatus");
}
