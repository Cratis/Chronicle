// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ProviderHosts;

internal sealed class DisposalHost(Func<ValueTask> dispose) : IStorageProviderHost
{
    int _disposalCount;

    public StorageProvider Provider => StorageProvider.Sqlite;
    public CommandCounter Counter { get; } = new();
    public int DisposalCount => _disposalCount;

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposalCount);
        return dispose();
    }

    public Task Start() => Task.CompletedTask;
    public IEventSequenceStorage CreateSequence() => throw new NotSupportedException();
    public Task<(IUniqueConstraintsStorage Storage, UniqueConstraintDefinition Definition)> CreateConstraints() => throw new NotSupportedException();
    public Task<SettingsLedger> BuildLedger(StorageRunOptions options, LoadReading loadAtStart) => throw new NotSupportedException();
}
