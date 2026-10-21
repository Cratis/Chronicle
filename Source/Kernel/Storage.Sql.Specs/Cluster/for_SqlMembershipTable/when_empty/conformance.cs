// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_empty;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _result;
    async Task Because() => _result = await Table.ReadAll();
    [Fact] public void should_have_no_members() => _result.Members.Count.ShouldEqual(0);
    [Fact] public void should_start_at_version_zero() => _result.Version.Version.ShouldEqual(0);
    [Fact] public void should_have_a_version_etag() => _result.Version.VersionEtag.ShouldEqual("0");
}
