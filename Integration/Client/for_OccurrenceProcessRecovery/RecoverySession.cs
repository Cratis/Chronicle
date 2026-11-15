// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class RecoverySession(string connection, string store) : IAsyncDisposable
{
    public const string Trigger = "process-recovery-work";
    readonly ChronicleClient _client = new(new ChronicleClientOptions { ConnectionString = new(connection), EventStore = store, AutoDiscoverAndRegister = false });
    public IEventStore Store { get; private set; } = null!;
    public Contracts.Occurrences.IOccurrences Service => ((IChronicleServicesAccessor)Store.Connection).Services.Occurrences;

    public async Task Connect()
    {
        Store = await _client.GetEventStore(store).WaitAsync(TimeSpan.FromSeconds(60));
    }

    public async Task<OccurrenceSnapshotResponse> WaitFor(string id, Func<OccurrenceSnapshotResponse, bool> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(150));
        var rows = await Service.ObserveOccurrences(new()
        {
            EventStore = store,
            Namespace = Store.Namespace,
            Trigger = Trigger
        }).Select(result => result.EnsureSuccess().ToArray())
            .FirstAsync(rows => rows.Any(row => row.Id == id && condition(row)))
            .ToTask(deadline.Token);
        return rows.Single(row => row.Id == id);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
