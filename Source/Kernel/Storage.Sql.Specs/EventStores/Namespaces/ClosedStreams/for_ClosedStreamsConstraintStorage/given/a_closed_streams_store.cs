// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.given;

public abstract class a_closed_streams_store(ClosedStreamsHarness harness) : Specification
{
    protected static readonly string[] _spellings =
    [
        "Order-1",
        "order-1",
        "Order-1 ",
        "ＯＲＤＥＲ-1",
        "Ørder-1",
        "Order-1\u0001"
    ];

    protected readonly ClosedStreamsHarness _harness = harness;

    protected ClosedStreamsConstraintStorage Storage => _harness.Storage;

    public static ClosedStreamsHarness Sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-closed-streams-{Guid.NewGuid():N}.db");
        return new("sqlite", () => Task.FromResult($"DataSource={path};Pooling=False;Default Timeout=30"), _ =>
        {
            File.Delete(path);
            return Task.CompletedTask;
        });
    }

    async Task Establish() => await _harness.Initialize();
    async Task Destroy() => await _harness.DisposeAsync();
}
