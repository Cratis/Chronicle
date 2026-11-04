// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_Append.when_handling;

public class and_the_event_sequence_refuses_a_reserved_name : Specification
{
    readonly CommandScenario<Append> _scenario = ChronicleCommandScenario.For<Append>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
        var sequence = Substitute.For<IEventSequence>();
        sequence.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<Concepts.Events.EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>())
            .Returns(_ => Task.FromException<AppendResult>(new ReservedEventSequenceName("observers")));
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(sequence);
        _scenario.Services.AddSingleton(factory);
        _scenario.Services.AddSingleton(new RequestCausation(new HttpContextAccessor()));
    }

    async Task Because() => _result = await _scenario.Execute(new Append("store", "namespace", "observers", "source", "Default", "All", "Default", new EventType("event", 1, false), "{}"));

    [Fact] void should_be_invalid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventSequenceId");
    [Fact] void should_report_the_message() => _result.ValidationResults.Select(result => result.Message).ShouldContain(ReservedEventSequenceRules.Message("observers"));
}
