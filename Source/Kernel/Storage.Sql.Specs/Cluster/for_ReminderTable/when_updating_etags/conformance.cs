// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_updating_etags;

public abstract class conformance(MembershipHarness harness) : Specification
{
    IReminderTable _table;
    ReminderEntry _entry;
    string _originalETag;
    string _newETag;

    async Task Establish()
    {
        await harness.Initialize(false);
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(harness.CreateContext(), () => { })));
        _table = new ReminderTable(database);
        _entry = new ReminderEntry
        {
            GrainId = GrainId.Create("observer", "etag-spec"),
            ReminderName = "retry",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(1)
        };
        _originalETag = (await _table.UpsertRow(_entry))!;
        _entry.ETag = _originalETag;
    }

    async Task Because() => _newETag = (await _table.UpsertRow(_entry))!;

    [Fact] public void should_mint_a_new_etag_even_when_one_was_supplied() => _newETag.ShouldNotEqual(_originalETag);
    [Fact] public async Task should_store_the_new_etag() => (await _table.ReadRow(_entry.GrainId, _entry.ReminderName))!.ETag.ShouldEqual(_newETag);
    [Fact] public async Task should_not_remove_with_the_old_etag() => (await _table.RemoveRow(_entry.GrainId, _entry.ReminderName, _originalETag)).ShouldBeFalse();
    [Fact] public async Task should_not_remove_with_a_wrong_etag() => (await _table.RemoveRow(_entry.GrainId, _entry.ReminderName, "wrong")).ShouldBeFalse();
    [Fact] public async Task should_remove_with_the_current_etag() => (await _table.RemoveRow(_entry.GrainId, _entry.ReminderName, _newETag)).ShouldBeTrue();
    [Fact] public async Task should_allow_only_one_concurrent_removal()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => _table.RemoveRow(_entry.GrainId, _entry.ReminderName, _newETag)));
        results.Count(_ => _).ShouldEqual(1);
    }

    async Task Destroy() => await harness.DisposeAsync();
}
