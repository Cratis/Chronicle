// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_reading_while_a_writer_commits;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool _result;
    MembershipTableData _view;
    async Task Because()
    {
        var original = await Insert();
        var interceptor = new WriteBetweenReads(async () =>
        {
            Entry.Status = SiloStatus.Active;
            (await Table.UpdateRow(Entry, original.Members.Single().Item2, original.Version.Next())).ShouldBeTrue();
        });
        _view = await WithInterceptors(interceptor).ReadAll();
        _result = await Table.UpdateRow(_view.Members.Single().Item1, _view.Members.Single().Item2, _view.Version.Next());
    }
    [Fact] public void should_hold_the_old_version() => _view.Version.Version.ShouldEqual(1);
    [Fact] public void should_observe_the_committed_member() => _view.Members.Single().Item1.Status.ShouldEqual(SiloStatus.Active);
    [Fact] public void should_reject_a_cas_from_the_stale_view() => _result.ShouldBeFalse();
}
