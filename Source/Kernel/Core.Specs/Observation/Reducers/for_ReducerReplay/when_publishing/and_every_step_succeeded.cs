// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_publishing;

public class and_every_step_succeeded : given.a_reducer_replay
{
    ReplayPublication _result;
    async Task Because() => _result = await _replay.Publish(_context);
    [Fact] void should_publish_the_isolated_target() => _live.Received(1).PublishReplay(_context, _shadow);
    [Fact] void should_record_the_published_occurrence() => _manager.Received(1).Replayed(_key.ObserverId, _context);
    [Fact] void should_report_publication() => _result.ShouldEqual(ReplayPublication.Published);
    [Fact] void should_keep_the_identity_for_recovery_of_a_lost_reply() => _current.ShouldNotBeNull();
}
