// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_publishing_an_isolated_replay;

public class and_a_flush_rpc_fails : given.an_isolated_publication
{
    Exception? _error;
    void Establish() => _target.EndBulk().Returns(Task.FromException<IEnumerable<FailedPartition>>(new IOException("flush unavailable")));
    async Task Because() => _error = await Catch.Exception(() => _sink.PublishReplay(_context, _target));
    [Fact] void should_not_promote_an_unproven_result() => _collections.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] void should_surface_the_flush_failure() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_switch_the_live_sink() => _collections.DidNotReceive().AbandonReplay();
}
