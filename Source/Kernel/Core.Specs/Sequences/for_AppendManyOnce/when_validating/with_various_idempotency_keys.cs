// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnce.when_validating;

public class with_various_idempotency_keys : Specification
{
    readonly CommandScenario<AppendManyOnce> _scenario = ChronicleCommandScenario.For<AppendManyOnce>();
    CommandResult _empty;
    CommandResult _whitespace;
    CommandResult _tooLong;
    CommandResult _longestAllowed;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    static AppendManyOnce With(string key) => new("some-event-store", "some-namespace", "event-log", [new KeyedEventForEventSourceId(new EventForEventSourceId("some-event-source", "Default", "All", "Default", new EventType("SomeEvent", 1, false), "{}"), key)]);

    async Task Because()
    {
        _empty = await _scenario.Validate(With(string.Empty));
        _whitespace = await _scenario.Validate(With("   "));
        _tooLong = await _scenario.Validate(With(new string('k', 513)));
        _longestAllowed = await _scenario.Validate(With(new string('k', 512)));
    }

    [Fact] void should_accept_an_empty_key_as_no_key() => _empty.ShouldBeValid();
    [Fact] void should_refuse_a_whitespace_only_key() => _whitespace.ShouldNotBeSuccessful();
    [Fact] void should_refuse_a_key_of_513_characters() => _tooLong.ShouldNotBeSuccessful();
    [Fact] void should_accept_a_key_of_512_characters() => _longestAllowed.ShouldBeValid();
}
