// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_publishing_an_isolated_replay;

public class and_the_flush_returns_failed_partitions : given.an_isolated_publication
{
    FailedPartition[] _failures;
    void Establish() => _target.EndBulk().Returns(Task.FromResult<IEnumerable<FailedPartition>>([new("source", 1UL)]));
    async Task Because() => _failures = (await _sink.PublishReplay(_context, _target)).ToArray();
    [Fact] void should_not_apply_projection_partial_publication_semantics_to_reducers() => _collections.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] void should_report_the_failure() => _failures.Length.ShouldEqual(1);
}
