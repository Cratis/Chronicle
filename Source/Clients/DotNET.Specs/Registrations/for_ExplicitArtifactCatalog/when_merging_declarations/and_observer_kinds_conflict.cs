// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_observer_kinds_conflict : given.a_catalog
{
    Exception _peerError;
    Exception _markerError;
    ExplicitArtifacts _markerDeclarations;

    void Establish()
    {
        _options.RegisterReactor<PlainArtifact>().RegisterReducer<PlainArtifact>();
        var marked = EmitType("MarkerReactor", interfaces: [typeof(IReactor)]);
        _markerDeclarations = new ExplicitArtifacts().RegisterReducer(marked);
    }

    void Because()
    {
        _peerError = Catch.Exception(() => _options.CreateSnapshot().ApplyTo(_discovered));
        _markerError = Catch.Exception(() => _markerDeclarations.CreateSnapshot().ApplyTo(_discovered));
    }

    [Fact] void should_reject_incompatible_explicit_kinds() => _peerError.ShouldBeOfExactType<ConflictingArtifactKind>();
    [Fact] void should_reject_a_kind_that_contradicts_the_marker() => _markerError.ShouldBeOfExactType<ConflictingArtifactKind>();
}
