// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder;

public class when_resolving_event_property_types : Specification
{
    ChildrenBuilder<Parent, Child> _builder;
    Contracts.Events.EventType _unmarkedIdentity;
    Contracts.Events.EventType _markedIdentity;
    Exception _unknownError;

    void Establish()
    {
        var registry = RegisteredEventTypesForSpecifications.Create(
            (typeof(Unmarked), new("explicit-unmarked", 3)),
            (typeof(Marked), typeof(Marked).GetEventType()));
        _builder = new(new DefaultNamingPolicy(), registry, new JsonSerializerOptions(), AutoMap.Inherit);
    }

    void Because()
    {
        _builder.FromEventProperty<Unmarked>(@event => @event.Child);
        _unmarkedIdentity = StoredIdentity();
        _builder.FromEventProperty<Marked>(@event => @event.Child);
        _markedIdentity = StoredIdentity();
        _unknownError = Catch.Exception(() => _builder.FromEventProperty<Unknown>(@event => @event.Child));
    }

    [Fact] void should_use_the_registered_unmarked_identity() => _unmarkedIdentity.Id.ShouldEqual("explicit-unmarked");
    [Fact] void should_use_the_registered_unmarked_generation() => _unmarkedIdentity.Generation.ShouldEqual(3U);
    [Fact] void should_preserve_the_attributed_identity() => _markedIdentity.Id.ShouldEqual("marked-child-event");
    [Fact] void should_preserve_the_attributed_generation() => _markedIdentity.Generation.ShouldEqual(2U);
    [Fact] void should_reject_unknown_types() => _unknownError.ShouldBeOfExactType<TypeIsNotAnEventType>();

    Contracts.Events.EventType StoredIdentity() => (Contracts.Events.EventType)typeof(ChildrenBuilder<Parent, Child>)
        .GetField("_fromEventPropertyEventType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_builder)!;

    public record Parent;
    public record Child(string Name);
    public record Unmarked(Child Child);
    public record Unknown(Child Child);
    [EventType("marked-child-event", 2)]
    public record Marked(Child Child);
}
