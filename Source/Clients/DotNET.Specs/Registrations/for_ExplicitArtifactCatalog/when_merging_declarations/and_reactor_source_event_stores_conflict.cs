// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_reactor_source_event_stores_conflict : given.a_catalog
{
    ExplicitArtifacts[] _declarations;
    Exception[] _errors;

    void Establish()
    {
        var marked = EmitType("MarkedReactor", [AttributeFor<EventStoreAttribute>([typeof(string)], "owner")]);
        var plain = EmitType("PlainReactor");
        var sequence = EmitType("SequenceReactor", [AttributeFor<EventSequenceAttribute>([typeof(string)], "custom")]);
        var reactorSequence = EmitType("ReactorSequence", [AttributeFor<ReactorAttribute>([typeof(string), typeof(string)], "", "custom")]);
        var eventLog = EmitType("EventLogReactor", [AttributeFor<EventLogAttribute>([])]);
        _declarations =
        [
            new ExplicitArtifacts().RegisterReactor(marked, ReactorRegistrationOptions.FromEventStore("different")),
            new ExplicitArtifacts().RegisterReactor(plain, ReactorRegistrationOptions.FromEventStore("first"))
                .RegisterReactor(plain, ReactorRegistrationOptions.FromEventStore("second")),
            new ExplicitArtifacts().RegisterReactor(sequence, ReactorRegistrationOptions.FromEventStore("host")),
            new ExplicitArtifacts().RegisterReactor(reactorSequence, ReactorRegistrationOptions.FromEventStore("host")),
            new ExplicitArtifacts().RegisterReactor(eventLog, ReactorRegistrationOptions.FromEventStore("host"))
        ];
    }

    void Because() => _errors = _declarations.Select(_ => Catch.Exception(() => _.CreateSnapshot().ApplyTo(_discovered))!).ToArray();

    [Fact] void should_reject_a_different_attribute() => _errors[0].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_different_explicit_stores() => _errors[1].ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_reject_an_explicit_event_sequence() => _errors[2].ShouldBeOfExactType<EventStoreCannotBeCombinedWithExplicitEventSequence>();
    [Fact] void should_reject_a_reactor_attribute_sequence() => _errors[3].ShouldBeOfExactType<EventStoreCannotBeCombinedWithExplicitEventSequence>();
    [Fact] void should_reject_an_explicit_event_log() => _errors[4].ShouldBeOfExactType<EventStoreCannotBeCombinedWithExplicitEventSequence>();
}
