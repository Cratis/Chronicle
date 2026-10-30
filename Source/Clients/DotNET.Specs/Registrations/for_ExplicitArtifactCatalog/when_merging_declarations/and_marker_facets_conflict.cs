// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_marker_facets_conflict : given.a_catalog
{
    Type _event;
    ExplicitArtifacts[] _declarations;
    Exception[] _errors;

    void Establish()
    {
        _event = EmitType("MarkedEvent",
        [
            AttributeFor<EventTypeAttribute>([typeof(string), typeof(uint)], "marked", 2u),
            AttributeFor<EventStoreAttribute>([typeof(string)], "owner"),
            AttributeFor<PublicAttribute>([]),
            AttributeFor<PublishToAttribute>([typeof(string)], "destination")
        ]);
        _discovered.EventTypes.Returns([_event]);
        _declarations =
        [
            new ExplicitArtifacts().RegisterEventType(_event, _ => _.WithId("different")),
            new ExplicitArtifacts().RegisterEventType(_event, _ => _.Generation(1)),
            new ExplicitArtifacts().RegisterEventType(_event, _ => _.ForEventStore("different")),
            new ExplicitArtifacts().RegisterEventType(_event, _ => _.Private()),
            new ExplicitArtifacts().RegisterEventType(_event, _ => _.PublishTo("different"))
        ];
    }

    void Because() => _errors = _declarations.Select(_ => Catch.Exception(() => _.CreateSnapshot().ApplyTo(_discovered))!).ToArray();

    [Fact] void should_reject_marker_identity_conflicts() => _errors[0].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_marker_generation_conflicts() => _errors[1].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_marker_ownership_conflicts() => _errors[2].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_marker_visibility_conflicts() => _errors[3].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_marker_destination_conflicts() => _errors[4].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
}
