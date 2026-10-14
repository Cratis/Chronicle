// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_live_closure_is_already_applied : given.a_replay
{
    DateTimeOffset? _liveClosedAt;

    async Task Establish()
    {
        Event(0, "source", "Closed");
        _events[0] = _events[0] with { Context = _events[0].Context with { Occurred = DateTimeOffset.Parse("2026-01-01T00:00:00Z") } };
        await new ClosesStreamIndexUpdater(_closing, new([], "source", "Closed", _events[0].Content), _closed).Update(0);
        _liveClosedAt = (await _closed.GetForOwner("closing")).Single().ClosedAt;
        _closureStorage = Substitute.For<IClosedStreamsConstraintStorage>();
        _closureStorage.GetForOwner(Arg.Any<ClosedStreamOwner>(), Arg.Any<CancellationToken>()).Returns(call => _closed.GetForOwner(call.ArgAt<ClosedStreamOwner>(0)));
        _closureStorage.GetFor(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>()).Returns(call => _closed.GetFor(call.ArgAt<ClosedStreamOwner>(0), call.ArgAt<ClosedStreamScope>(1)));
        _closureStorage.Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _writes.Add("close");
            return _closed.Close(call.ArgAt<ClosedStream>(0));
        });
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_not_rewrite_the_live_closure() => _writes.ShouldBeEmpty();
    [Fact] async Task should_preserve_the_live_timestamp() => (await _closed.GetForOwner("closing")).Single().ClosedAt.ShouldEqual(_liveClosedAt);
}
