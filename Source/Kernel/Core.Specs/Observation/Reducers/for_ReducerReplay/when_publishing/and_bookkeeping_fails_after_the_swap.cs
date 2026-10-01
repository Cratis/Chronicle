// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_publishing;

public class and_bookkeeping_fails_after_the_swap : given.a_reducer_replay
{
    ReplayPublication _result;
    void Establish() => _manager.Replayed(_key.ObserverId, _context).Returns(Task.FromException(new IOException("bookkeeping unavailable")));
    async Task Because() => _result = await _replay.Publish(_context);
    [Fact] void should_not_misreport_the_model_as_unpublished() => _result.ShouldEqual(ReplayPublication.PublishedWithBookkeepingFailure);
    [Fact] void should_have_committed_the_swap() => _live.Received(1).PublishReplay(_context, _shadow);
    [Fact] void should_retain_the_identity_for_retry() => _current.ShouldNotBeNull();
}
