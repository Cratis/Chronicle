// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_losing_the_commit_acknowledgment;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool _result;
    MembershipTableData _after;
    readonly CommitAcknowledgmentFailure _failure = new();
    async Task Because()
    {
        var current = await Table.ReadAll();
        _result = await WithInterceptors(_failure).InsertRow(Entry, current.Version.Next());
        _after = await Table.ReadAll();
    }
    [Fact] public void should_recognize_the_committed_insert() => _result.ShouldBeTrue();
    [Fact] public void should_verify_instead_of_retrying() => _failure.CommitCount.ShouldEqual(1);
    [Fact] public void should_store_one_member() => _after.Members.Count.ShouldEqual(1);
    [Fact] public void should_bump_the_version_once() => _after.Version.Version.ShouldEqual(1);
}
