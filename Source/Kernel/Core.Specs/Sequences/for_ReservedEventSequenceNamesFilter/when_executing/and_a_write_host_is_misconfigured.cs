// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_a_write_host_is_misconfigured : given.a_reserved_event_sequence_names_filter
{
    readonly CommandScenario<Append> _scenario = ChronicleCommandScenario.For<Append>();
    IGrainFactory _grainFactory;
    CommandResult _result;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(_storage);
        _scenario.Services.AddSingleton(_grainFactory);
        _scenario.Services.RemoveAll<IReservedEventSequenceIds>();
    }

    async Task Because() => _result = await _scenario.Execute(AppendTo("orders"));

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_missing_configuration() => _result.ExceptionMessages.Any(message => message.Contains(nameof(IReservedEventSequenceIds))).ShouldBeTrue();
    [Fact] void should_not_reach_the_handler() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_get_storage_for_writing() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
}
