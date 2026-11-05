// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

public class when_upgrading_on_sqlite : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    List<string> _before;
    List<string> _after;
    List<string> _flags;
    readonly string _path = Path.Combine(Path.GetTempPath(), $"chronicle-closed-{Guid.NewGuid():N}.db");

    async Task Establish()
    {
        _connectionString = await Task.FromResult($"DataSource={_path};Pooling=False;Default Timeout=30");
        _harness = new(_connectionString, sqlite: true);
        await _harness.PrepareEarlierLayout("Alice", "bob ");
        _before = await _harness.Spellings();
    }

    async Task Because()
    {
        await _harness.Apply(true);
        _after = await _harness.Spellings();
        _flags = await _harness.Flags();
    }

    Task Destroy()
    {
        File.Delete(_path);
        return Task.CompletedTask;
    }

    [Fact] void should_keep_every_row() => _after.Count.ShouldEqual(2);
    [Fact] void should_keep_every_stored_value_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_mark_old_rows_as_exact() => _flags.ShouldEqual(["1", "1"]);
}
