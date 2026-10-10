// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4177;

/// <summary>
/// A note was taken.
/// </summary>
/// <param name="Text">The text.</param>
[EventType]
public record Note4177Taken(string Text);

/// <summary>
/// The discarded fact should FAIL (the predicate is false). If it passes, the assertion is vacuous.
/// </summary>
public class when_an_assertion_task_is_discarded : Specification, IDisposable
{
    EventScenario _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new EventScenario();
        _id = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new Note4177Taken("real"));

    [Fact] void should_fail_because_the_predicate_is_false_but_is_discarded() => _scenario.EventLog.ShouldHaveAppendedEvent<Note4177Taken>(_id, _ => _.Text == "never");

    [Fact] async Task awaited_control_throws() => (await Catch.Exception(async () => await _scenario.EventLog.ShouldHaveAppendedEvent<Note4177Taken>(_id, _ => _.Text == "never"))).ShouldNotBeNull();

    public void Dispose() => _scenario.Dispose();
}
