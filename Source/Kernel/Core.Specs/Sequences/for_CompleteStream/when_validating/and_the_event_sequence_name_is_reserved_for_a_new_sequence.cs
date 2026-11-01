// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_CompleteStream.when_validating;

public class and_the_event_sequence_name_is_reserved_for_a_new_sequence : Specification
{
    readonly CommandScenario<CompleteStream> _scenario = ChronicleCommandScenario.For<CompleteStream>();
    CommandResult _result;

    void Establish()
    {
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([]));
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([(EventStoreName)"some-event-store"]));
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        var reserved = Substitute.For<IReservedEventSequenceIds>();
        reserved.IsReserved(new EventSequenceId("observers")).Returns(true);
        _scenario.Services.AddSingleton(storage);
        _scenario.Services.AddSingleton(reserved);
    }

    async Task Because() => _result = await _scenario.Validate(new CompleteStream(
        "some-event-store",
        "some-namespace",
        "observers",
        "All",
        "Default"));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_sequence_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventSequenceId");
    [Fact] void should_name_the_reserved_event_sequence() => _result.ValidationResults.Select(result => result.Message).ShouldContain(ReservedEventSequenceRules.Message("observers"));
}
