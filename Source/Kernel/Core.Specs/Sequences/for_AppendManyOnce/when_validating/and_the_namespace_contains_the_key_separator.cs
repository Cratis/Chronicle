// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnce.when_validating;

public class and_the_namespace_contains_the_key_separator : Specification
{
    readonly CommandScenario<AppendManyOnce> _scenario = ChronicleCommandScenario.For<AppendManyOnce>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new AppendManyOnce(
        "some-event-store",
        "Default#x",
        "event-log",
        [new KeyedEventForEventSourceId(new EventForEventSourceId("some-event-source", "Default", "All", "Default", new EventType("SomeEvent", 1, false), "{}"), "some-key")]));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_namespace_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("namespace");
    [Fact] void should_report_the_separator_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The value must not contain '#'.");
}
