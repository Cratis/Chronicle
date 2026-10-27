// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendOnce.when_validating;

public class and_the_event_store_is_the_system_event_store : Specification
{
    readonly CommandScenario<AppendOnce> _scenario = ChronicleCommandScenario.For<AppendOnce>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new AppendOnce(
        "System",
        "some-namespace",
        "event-log",
        "some-event-source",
        "Default",
        "All",
        "Default",
        new EventType("SomeEvent", 1, false),
        "{}",
        "some-key"));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_store_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_reserved_store_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The System event store is reserved for the kernel and cannot be written through the public API.");
}
