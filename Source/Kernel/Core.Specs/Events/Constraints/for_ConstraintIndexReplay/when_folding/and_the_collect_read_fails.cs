// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_collect_read_fails : given.a_replay
{
    Exception _error;

    void Establish()
    {
        Event(5, "source", "Claimed", "value");
        _unique.Configure().GetFor(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IEnumerable<UniqueConstraintValue>>(new IOException("index unavailable")));
        Start();
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _replay.Continue());

    [Fact] void should_report_in_the_result_instead_of_throwing() => _error.ShouldBeNull();
    [Fact] void should_return_a_pending_result() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_fail_only_the_affected_constraint() => _result.Failed.Keys.Select(name => name.Value).ShouldContainOnly("unique");
    [Fact] void should_identify_the_deciding_event() => _result.Failed[_definition.Name].Position!.Value.ShouldEqual(5UL);
}
