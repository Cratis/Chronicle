// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_schema_read_fails : given.a_replay
{
    Exception _error;

    void Establish()
    {
        Event(7, "source", "Claimed", "value");
        _types.Configure().GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(Task.FromException<EventTypeSchema>(new IOException("schema unavailable")));
        Start();
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _replay.Continue());

    [Fact] void should_report_in_the_result_instead_of_throwing() => _error.ShouldBeNull();
    [Fact] void should_report_the_failed_scopes() => _result.Failed.Keys.Select(name => name.Value).ShouldContainOnly("unique", "closing");
    [Fact] void should_identify_the_failing_event() => _result.Failed.Values.ShouldEachConformTo(failure => failure.Position == (EventSequenceNumber)7);
    [Fact] void should_preserve_the_error_type() => _result.Failed.Values.ShouldEachConformTo(failure => failure.Error.GetType() == typeof(IOException));
}
