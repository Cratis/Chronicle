// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.for_ArtifactIdentity;

public class when_deriving_identities : Specification
{
    EventType _defaultEvent;
    EventType _explicitEvent;
    EventType _inheritedEvent;
    EventType _historicalEvent;
    string _nestedObserver;
    string _genericObserver;
    string _explicitObserver;
    string _readModel;
    string _source;
    string _suffixOnlySource;
    string _explicitSource;
    string _propertyConstraint;
    string _typeConstraint;
    string _explicitConstraint;

    void Because()
    {
        _defaultEvent = ArtifactIdentity.EventTypeFor(typeof(DefaultEvent));
        _explicitEvent = ArtifactIdentity.EventTypeFor(typeof(ExplicitEvent));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"InheritedIdentity_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var inheritedType = assembly.DefineDynamicModule("Main").DefineType("InheritedEvent", TypeAttributes.Public, typeof(ExplicitEvent)).CreateType()!;
        _inheritedEvent = ArtifactIdentity.EventTypeFor(inheritedType);
        _historicalEvent = ArtifactIdentity.EventTypeFor(typeof(Previous));
        _nestedObserver = ArtifactIdentity.ObserverIdFor(typeof(DefaultEvent), null);
        _genericObserver = ArtifactIdentity.ObserverIdFor(typeof(Generic<>), "");
        _explicitObserver = ArtifactIdentity.ObserverIdFor(typeof(DefaultEvent), "explicit-observer");
        _readModel = ArtifactIdentity.ReadModelIdFor(typeof(DefaultEvent));
        _source = ArtifactIdentity.EventSourceNameFor(typeof(OrdersEventSource));
        _suffixOnlySource = ArtifactIdentity.EventSourceNameFor(typeof(EventSource));
        _explicitSource = ArtifactIdentity.EventSourceNameFor(typeof(NamedEventSource));
        _propertyConstraint = ArtifactIdentity.ConstraintNameFor(typeof(DefaultEvent).GetProperty(nameof(DefaultEvent.Name))!, null);
        _typeConstraint = ArtifactIdentity.ConstraintNameFor(typeof(DefaultEvent), null);
        _explicitConstraint = ArtifactIdentity.ConstraintNameFor(typeof(DefaultEvent), "");
    }

    [Fact] void should_use_the_event_simple_name() => _defaultEvent.ShouldEqual(new EventType("DefaultEvent", 1));
    [Fact] void should_preserve_explicit_event_identity() => _explicitEvent.ShouldEqual(new EventType("explicit-event", 3));
    [Fact] void should_preserve_inherited_event_metadata() => _inheritedEvent.ShouldEqual(new EventType("explicit-event", 3));
    [Fact] void should_use_the_referenced_id_for_history() => _historicalEvent.ShouldEqual(new EventType("explicit-event", 1));
    [Fact] void should_preserve_nested_observer_names() => _nestedObserver.ShouldEqual("Cratis.Chronicle.for_ArtifactIdentity.when_deriving_identities+DefaultEvent");
    [Fact] void should_preserve_generic_observer_names() => _genericObserver.ShouldEqual("Cratis.Chronicle.for_ArtifactIdentity.when_deriving_identities+Generic`1");
    [Fact] void should_preserve_explicit_observer_ids() => _explicitObserver.ShouldEqual("explicit-observer");
    [Fact] void should_preserve_read_model_names() => _readModel.ShouldEqual("Cratis.Chronicle.for_ArtifactIdentity.when_deriving_identities+DefaultEvent");
    [Fact] void should_strip_the_event_source_suffix() => _source.ShouldEqual("Orders");
    [Fact] void should_not_strip_the_entire_event_source_name() => _suffixOnlySource.ShouldEqual("EventSource");
    [Fact] void should_preserve_explicit_empty_source_names() => _explicitSource.ShouldEqual("");
    [Fact] void should_use_the_property_name_for_a_property_constraint() => _propertyConstraint.ShouldEqual("Name");
    [Fact] void should_use_the_type_name_for_a_type_constraint() => _typeConstraint.ShouldEqual("DefaultEvent");
    [Fact] void should_preserve_an_explicit_empty_constraint_name() => _explicitConstraint.ShouldEqual("");

    public record DefaultEvent(string Name);
    [EventType("explicit-event", 3)]
    public class ExplicitEvent;
    [EventTypeGenerationFor<ExplicitEvent>(1)]
    public record Previous;
    class Generic<T>;
    class OrdersEventSource;
    class EventSource;
    [EventSource("")]
    class NamedEventSource;
}
