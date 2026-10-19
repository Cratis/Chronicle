// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_one_write_is_abandoned_and_another_fails : given.a_replay
{
    TaskCompletionSource _hung;

    void Establish()
    {
        Event(0, "first", "Claimed", "first");
        Event(1, "second", "Claimed", "second");
        Event(2, "closed", "Closed");
        _hung = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unique.Configure().Save("first", _definition, 0, Hash("first"), string.Empty, Arg.Any<Subject?>(), Arg.Any<CancellationToken>()).Returns(_hung.Task);
        _closureStorage = Substitute.For<IClosedStreamsConstraintStorage>();
        _closureStorage.Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("closure write failed")));
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    void Destroy() => _hung.TrySetResult();

    [Fact] void should_report_the_abandoned_constraint() => _result.Abandoned.Select(name => name.Value).ShouldContainOnly("unique");
    [Fact] void should_report_only_the_failed_constraint() => _result.Failed.Keys.Select(name => name.Value).ShouldContainOnly("closing");
    [Fact] void should_preserve_the_failed_writes_error() => _result.Failed[_closing.Name].Error.ShouldBeOfExactType<IOException>();
    [Fact] void should_preserve_the_failed_writes_position() => _result.Failed[_closing.Name].Position!.Value.ShouldEqual(2UL);
    [Fact] async Task should_stop_later_writes_for_the_abandoned_constraint() => await _unique.DidNotReceive().Save("second", _definition, 1, Hash("second"), string.Empty, Arg.Any<Subject?>(), Arg.Any<CancellationToken>());
}
