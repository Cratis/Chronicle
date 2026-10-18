// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_two_constraints_fail_independently : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Claimed", "value");
        Event(1, "source", "Closed");
        _unique.Configure().Save("source", _definition, 0, Hash("value"), string.Empty, Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("claim failed")));
        _closureStorage = Substitute.For<IClosedStreamsConstraintStorage>();
        _closureStorage.Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new TimeoutException("closure failed")));
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_report_both_constraints() => _result.Failed.Keys.Select(name => name.Value).ShouldContainOnly("unique", "closing");
    [Fact] void should_preserve_the_unique_error() => _result.Failed[_definition.Name].Error.ShouldBeOfExactType<IOException>();
    [Fact] void should_preserve_the_unique_position() => _result.Failed[_definition.Name].Position!.Value.ShouldEqual(0UL);
    [Fact] void should_preserve_the_closing_error() => _result.Failed[_closing.Name].Error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_preserve_the_closing_position() => _result.Failed[_closing.Name].Position!.Value.ShouldEqual(1UL);
}
