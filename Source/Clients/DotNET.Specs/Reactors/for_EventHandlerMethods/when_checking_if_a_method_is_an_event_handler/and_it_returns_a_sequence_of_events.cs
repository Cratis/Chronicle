// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_EventHandlerMethods.when_checking_if_a_method_is_an_event_handler;

/// <summary>
/// The side-effect handlers test the returned value with 'value is IEnumerable&lt;object&gt;', so an array or a
/// List is appended perfectly well once it reaches them. This check ran first and matched the open generic
/// exactly, so a handler returning one was not recognized as a handler at all - the reactor silently never
/// fired for that event (#4121).
/// </summary>
public class and_it_returns_a_sequence_of_events : Specification
{
    static readonly Type[] _eventTypes = [typeof(TheEvent)];

    IDictionary<string, bool> _results;

    void Because() => _results = typeof(TheReactor)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .ToDictionary(method => method.Name, method => method.IsEventHandlerMethod(_eventTypes));

    [Fact] void should_recognize_an_array() => _results["ReturningAnArray"].ShouldBeTrue();
    [Fact] void should_recognize_a_list() => _results["ReturningAList"].ShouldBeTrue();
    [Fact] void should_recognize_a_read_only_list() => _results["ReturningAReadOnlyList"].ShouldBeTrue();
    [Fact] void should_recognize_an_enumerable() => _results["ReturningAnEnumerable"].ShouldBeTrue();
    [Fact] void should_recognize_a_sequence_of_event_for_event_source_id() => _results["ReturningEventsForEventSourceIds"].ShouldBeTrue();
    [Fact] void should_recognize_a_single_event() => _results["ReturningASingleEvent"].ShouldBeTrue();
    [Fact] void should_not_recognize_an_unrelated_return_type() => _results["ReturningSomethingElse"].ShouldBeFalse();
    [Fact] void should_not_recognize_a_string() => _results["ReturningAString"].ShouldBeFalse();

    public record TheEvent(string Something);

    public class TheReactor
    {
        public TheEvent[] ReturningAnArray(TheEvent @event) => [];
        public List<TheEvent> ReturningAList(TheEvent @event) => [];
        public IReadOnlyList<TheEvent> ReturningAReadOnlyList(TheEvent @event) => [];
        public IEnumerable<TheEvent> ReturningAnEnumerable(TheEvent @event) => [];
        public EventForEventSourceId[] ReturningEventsForEventSourceIds(TheEvent @event) => [];
        public TheEvent ReturningASingleEvent(TheEvent @event) => @event;
        public int ReturningSomethingElse(TheEvent @event) => 42;
        public string ReturningAString(TheEvent @event) => string.Empty;
    }
}
