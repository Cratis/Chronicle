// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot;

public class when_committing : given.a_stateless_aggregate_root
{
    AggregateRootCommitResult _expectedResult;
    AggregateRootCommitResult _result;

    void Establish()
    {
        _expectedResult = AggregateRootCommitResult.Successful();
        _mutation.Commit().Returns(Task.FromResult(_expectedResult));
    }

    async Task Because() => _result = await _aggregateRoot.Commit();

    [Fact] void should_call_commit_on_mutation() => _mutation.Received(1).Commit();
    [Fact] void should_return_the_result_from_mutation() => _result.ShouldEqual(_expectedResult);
    [Fact] void dehydrate_mutator() => _mutator.Received(1).Dehydrate();
}
