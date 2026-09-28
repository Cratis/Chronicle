// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendWithNamedTags.given;

public class an_append_with_named_tags_validation : Specification
{
    protected readonly CommandScenario<AppendWithNamedTags> _scenario = ChronicleCommandScenario.For<AppendWithNamedTags>();
    protected CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    protected static AppendWithNamedTags ValidCommand() => new(
        "some-event-store",
        "some-namespace",
        "event-log",
        "some-event-source",
        "Default",
        "All",
        "Default",
        new EventType("SomeEvent", 1, false),
        "{}",
        [new NamedTag("account", "one")]);
}
