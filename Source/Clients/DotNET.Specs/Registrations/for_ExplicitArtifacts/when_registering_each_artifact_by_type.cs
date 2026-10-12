// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifacts;

public class when_registering_each_artifact_by_type : Specification
{
    ExplicitArtifacts _generic;
    ExplicitArtifacts _byType;
    ExplicitArtifacts _returned;
    Type[] _types = [typeof(Event), typeof(Reactor), typeof(Reducer), typeof(Constraint), typeof(Source), typeof(Migration)];

    void Establish() => _generic = new ExplicitArtifacts()
        .RegisterEventType<Event>(_ => _.WithId("event").Generation(2).Public().ForEventStore("store").PublishTo("destination"))
        .RegisterReactor<Reactor>("reactor")
        .RegisterReducer<Reducer>("reducer")
        .RegisterConstraint<Constraint>()
        .RegisterEventSource<Source>("source")
        .RegisterMigration<Migration>();

    void Because()
    {
        _byType = new();
        _returned = _byType
            .RegisterEventType(_types[0], _ => _.WithId("event").Generation(2).Public().ForEventStore("store").PublishTo("destination"))
            .RegisterReactor(_types[1], "reactor")
            .RegisterReducer(_types[2], "reducer")
            .RegisterConstraint(_types[3])
            .RegisterEventSource(_types[4], "source")
            .RegisterMigration(_types[5]);
    }

    [Fact] void should_capture_identical_event_facets() => _byType.CreateSnapshot().EventTypes.ShouldContainOnly(_generic.CreateSnapshot().EventTypes.ToArray());
    [Fact] void should_capture_identical_artifact_nominations() => _byType.CreateSnapshot().Types.ShouldContainOnly(_generic.CreateSnapshot().Types.ToArray());
    [Fact] void should_return_the_same_options_object() => ReferenceEquals(_returned, _byType).ShouldBeTrue();

    public record Event;

    public class Reactor;

    public class Reducer;

    public class Constraint;

    public class Source;

    public class Migration;
}
