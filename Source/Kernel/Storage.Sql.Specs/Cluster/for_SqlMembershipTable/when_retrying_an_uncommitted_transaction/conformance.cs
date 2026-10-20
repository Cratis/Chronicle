// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_retrying_an_uncommitted_transaction;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool _result;
    MembershipTableData _after;
    readonly CommitBeforeFailure _failure = new();
    async Task Because()
    {
        _result = await WithInterceptors(_failure).InsertRow(Entry, (await Table.ReadAll()).Version.Next());
        _after = await Table.ReadAll();
    }
    [Fact] public void should_retry_after_rolling_back() => _result.ShouldBeTrue();
    [Fact] public void should_attempt_two_commits() => _failure.CommitCount.ShouldEqual(2);
    [Fact] public void should_insert_exactly_once() => _after.Members.Count.ShouldEqual(1);
    [Fact] public void should_increment_exactly_once() => _after.Version.Version.ShouldEqual(1);
}
