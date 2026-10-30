// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_explicit_facets_conflict : given.a_catalog
{
    ExplicitArtifacts[] _declarations;
    Exception[] _errors;

    void Establish() => _declarations =
    [
        new ExplicitArtifacts().RegisterEventType<PlainEvent>(_ => _.WithId("first")).RegisterEventType<PlainEvent>(_ => _.WithId("second")),
        new ExplicitArtifacts().RegisterEventType<PlainEvent>(_ => _.Generation(1)).RegisterEventType<PlainEvent>(_ => _.Generation(2)),
        new ExplicitArtifacts().RegisterEventType<PlainEvent>(_ => _.ForEventStore("first")).RegisterEventType<PlainEvent>(_ => _.ForEventStore("second")),
        new ExplicitArtifacts().RegisterEventType<PlainEvent>(_ => _.Public()).RegisterEventType<PlainEvent>(_ => _.Private()),
        new ExplicitArtifacts().RegisterEventType<PlainEvent>(_ => _.PublishTo("first")).RegisterEventType<PlainEvent>(_ => _.PublishTo("second"))
    ];

    void Because() => _errors = _declarations.Select(_ => Catch.Exception(() => _.CreateSnapshot().ApplyTo(_discovered))!).ToArray();

    [Fact] void should_reject_incompatible_ids() => _errors[0].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_incompatible_generations() => _errors[1].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_incompatible_ownership() => _errors[2].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_incompatible_visibility() => _errors[3].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_incompatible_destinations() => _errors[4].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
}
