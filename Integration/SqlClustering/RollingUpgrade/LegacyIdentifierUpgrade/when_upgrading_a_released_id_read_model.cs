// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.LegacyIdentifierUpgrade;

[Collection("RollingUpgrade")]
public class when_upgrading_a_released_id_read_model : Specification
{
    readonly ReleasedSqlKernelFixture _fixture = new(traceStatements: true, run: Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_TEST_RUN_ID") ?? $"legacy-id-{Guid.NewGuid():N}"[..20]);
    readonly CancellationTokenSource _deadline = new(TimeSpan.FromSeconds(420));
    LegacyIdentifierClient? _client;
    LegacyIdentifierDatabase _database = null!;
    LegacyIdentifierDatabase.Snapshot _released = null!;
    string _image = string.Empty;
    ulong _tail;
    bool _completed;

    async Task Establish()
    {
        // Admission supplies an image built from the accepted producer; never infer it from a floating tag.
        _image = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_image)) throw new InvalidOperationException("CRATIS_CHRONICLE_LOCAL_IMAGE must identify the provenance-checked #4590 image.");
        await _fixture.Start(_deadline.Token);
        _client = new(_fixture.ConnectionString);
        await _client.Register(_deadline.Token);
        var before = await _fixture.Database.ServerLogs();
        var first = await _client.Append("sentinel", "released sentinel", _deadline.Token);
        _tail = await _client.Append("existing", "released existing", _deadline.Token);
        _tail.ShouldBeGreaterThan(first);
        _database = new(_fixture.Database);
        await _database.Discover(_deadline.Token);
        _released = await _database.Capture(_deadline.Token);
        LegacyIdentifierDatabase.AssertLegacy(_released);
        AssertRows(_released, ("existing", "released existing"), ("sentinel", "released sentinel"));
        _released.Rows.All(row => row.Identifier is null).ShouldBeTrue();
        _database.AssertTrace(before, await _fixture.Database.ServerLogs(), "INSERT");
    }

    async Task Because()
    {
        var before = await _fixture.Database.ServerLogs();
        _client!.Dispose();
        _client = null;
        await _fixture.StartCurrent(_image, _deadline.Token);
        _client = new(_fixture.ConnectionString);
        await _client.Register(_deadline.Token);
        await _client.Read("existing", "released existing", _deadline.Token);
        await _client.Read("sentinel", "released sentinel", _deadline.Token);
        var readOnly = await _database.Capture(_deadline.Token);
        LegacyIdentifierDatabase.AssertRetained(_released, readOnly);
        ShouldCollectionExtensions.ShouldEqual(readOnly.Rows, _released.Rows.ToArray());
        _database.AssertTrace(before, await _fixture.Database.ServerLogs(), "SELECT");
        await WriteBeforeReplay();
        await ReplayAndWriteAgain();
        _completed = true;
    }

    async Task WriteBeforeReplay()
    {
        var before = await _fixture.Database.ServerLogs();
        var updated = await _client!.Append("existing", "current update", _deadline.Token);
        updated.ShouldBeGreaterThan(_tail);
        _tail = await _client.Append("new", "current insert", _deadline.Token);
        _tail.ShouldBeGreaterThan(updated);
        await _client.Read("existing", "current update", _deadline.Token);
        await _client.Read("new", "current insert", _deadline.Token);
        var written = await _database.Capture(_deadline.Token);
        LegacyIdentifierDatabase.AssertRetained(_released, written);
        AssertRows(written, ("existing", "current update"), ("new", "current insert"), ("sentinel", "released sentinel"));
        _database.AssertTrace(before, await _fixture.Database.ServerLogs(), "SELECT", "UPDATE", "INSERT");
    }

    async Task ReplayAndWriteAgain()
    {
        // Explicit replay may replace the physical table, unlike the upgrade/read/write checkpoints.
        var beforeReplay = await _database.Capture(_deadline.Token);
        await _client!.Replay(_deadline.Token);
        LegacyIdentifierDatabase.Snapshot replayed;
        do
        {
            await Task.Delay(100, _deadline.Token);
            replayed = await _database.Capture(_deadline.Token);
        }
        while (replayed.Relation == beforeReplay.Relation);
        await _client.WaitForCompletion(_tail, _deadline.Token);
        LegacyIdentifierDatabase.AssertRetained(beforeReplay, replayed, physical: false);
        AssertRows(replayed, ("existing", "current update"), ("new", "current insert"), ("sentinel", "released sentinel"));
        await _client.Read("sentinel", "released sentinel", _deadline.Token);
        await _client.Read("existing", "current update", _deadline.Token);
        await _client.Read("new", "current insert", _deadline.Token);
        var postReplay = await _client.Append("new", "after promotion", _deadline.Token);
        postReplay.ShouldBeGreaterThan(_tail);
        await _client.Read("new", "after promotion", _deadline.Token);
        var final = await _database.Capture(_deadline.Token);
        LegacyIdentifierDatabase.AssertRetained(replayed, final, physical: false);
        AssertRows(final, ("existing", "current update"), ("new", "after promotion"), ("sentinel", "released sentinel"));
    }

    static void AssertRows(LegacyIdentifierDatabase.Snapshot snapshot, params (string Id, string? Name)[] expected) =>
        snapshot.Rows.Select(row => (row.Id, row.Name)).ShouldEqual(expected);

    async Task Destroy()
    {
        try
        {
            _client?.Dispose();
        }
        finally
        {
            await _fixture.DisposeAsync();
            _deadline.Dispose();
        }
    }

    [Fact] void should_keep_the_released_key_layout_through_upgrade_and_replay() => _completed.ShouldBeTrue();
}
