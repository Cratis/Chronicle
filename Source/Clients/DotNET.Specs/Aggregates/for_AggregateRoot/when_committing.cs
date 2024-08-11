// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot;

public class when_committing : given.a_stateless_aggregate_root
{
    AggregateRootCommitResult expected_result;
    AggregateRootCommitResult result;

    void Establish()
    {
        expected_result = AggregateRootCommitResult.Successful();
        _mutation.Commit().Returns(Task.FromResult(expected_result));
    }

    async Task Because() => result = await _aggregateRoot.Commit();

    [Fact] void should_call_commit_on_mutation() => _mutation.Received(1).Commit();
    [Fact] void should_return_the_result_from_mutation() => result.ShouldEqual(expected_result);
    [Fact] void dehydrate_mutator() => _mutator.Received(1).Dehydrate();
}
