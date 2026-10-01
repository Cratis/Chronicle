// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_publishing;

public class and_the_swap_rpc_fails : given.a_reducer_replay
{
    Exception? _error;
    void Establish() => _live.PublishReplay(_context, _shadow).Returns(Task.FromException<IEnumerable<FailedPartition>>(new IOException("reply lost")));
    async Task Because() => _error = await Catch.Exception(() => _replay.Publish(_context));
    [Fact] void should_keep_the_outcome_unknown() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_remove_the_recovery_identity() => _contexts.DidNotReceive().Evict(Arg.Any<ReadModelIdentifier>());
    [Fact] void should_not_record_successful_bookkeeping() => _manager.DidNotReceive().Replayed(_key.ObserverId, _context);
}
