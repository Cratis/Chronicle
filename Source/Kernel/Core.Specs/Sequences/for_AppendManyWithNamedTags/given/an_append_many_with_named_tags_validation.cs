// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendManyWithNamedTags.given;

public class an_append_many_with_named_tags_validation : Specification
{
    protected readonly CommandScenario<AppendManyWithNamedTags> _scenario = ChronicleCommandScenario.For<AppendManyWithNamedTags>();
    protected CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    protected static AppendManyWithNamedTags ValidCommand() => new(
        "some-event-store",
        "some-namespace",
        "event-log",
        "some-event-source",
        [EventWith(new NamedTag("account", "one"))]);

    protected static EventToAppendWithNamedTags EventWith(params NamedTag[] namedTags) =>
        new(new EventType("SomeEvent", 1, false), "{}", namedTags);
}
