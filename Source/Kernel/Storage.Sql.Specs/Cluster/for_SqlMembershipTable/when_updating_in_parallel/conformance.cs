// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_updating_in_parallel;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool[] _results;
    MembershipTableData _after;
    string _originalEtag;
    async Task Because()
    {
        var current = await Insert();
        _originalEtag = current.Members.Single().Item2;
        Entry.Status = SiloStatus.Active;
        _results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Table.UpdateRow(Entry, _originalEtag, current.Version.Next())));
        _after = await Table.ReadAll();
    }
    [Fact] public void should_have_exactly_one_winner() => _results.Count(result => result).ShouldEqual(1);
    [Fact] public void should_increment_the_version_once() => _after.Version.Version.ShouldEqual(2);
    [Fact] public void should_change_the_row_etag() => (_after.Members.Single().Item2 != _originalEtag).ShouldBeTrue();
    [Fact] public void should_write_the_winner() => _after.Members.Single().Item1.Status.ShouldEqual(SiloStatus.Active);
}
