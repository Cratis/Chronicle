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
        expected_result = new AggregateRootCommitResult(true, ImmutableList<object>.Empty);
        mutation.Setup(_ => _.Commit()).Returns(Task.FromResult(expected_result));
    }

    async Task Because() => result = await aggregate_root.Commit();

    [Fact] void should_call_commit_on_mutation() => mutation.Verify(_ => _.Commit(), Once);
    [Fact] void should_return_the_result_from_mutation() => result.ShouldEqual(expected_result);
}
