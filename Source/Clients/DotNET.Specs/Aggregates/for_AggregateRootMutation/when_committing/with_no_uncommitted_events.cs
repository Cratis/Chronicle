// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutation.when_committing;

public class with_no_uncommitted_events : given.an_aggregate_mutation
{
    AggregateRootCommitResult _result;

    async Task Because() => _result = await _mutation.Commit();

    [Fact] void should_return_a_successful_commit_result() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_append_any_events_to_the_event_sequence() => _eventSequence.DidNotReceive().AppendMany(_eventSourceId, Arg.Any<IEnumerable<object>>());
}
