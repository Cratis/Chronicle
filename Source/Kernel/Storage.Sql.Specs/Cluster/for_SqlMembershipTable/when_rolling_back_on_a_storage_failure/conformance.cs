// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_rolling_back_on_a_storage_failure;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    Exception _error;
    MembershipTableData _after;
    async Task Because()
    {
        Entry.HostName = new string('x', 151);
        try
        {
            await Table.InsertRow(Entry, (await Table.ReadAll()).Version.Next());
        }
        catch (Exception error)
        {
            _error = error;
        }
        _after = await Table.ReadAll();
    }
    [Fact] public void should_propagate_the_storage_failure() => _error.ShouldNotBeNull();
    [Fact] public void should_roll_back_the_version() => _after.Version.Version.ShouldEqual(0);
    [Fact] public void should_not_insert_a_member() => _after.Members.Count.ShouldEqual(0);
}
