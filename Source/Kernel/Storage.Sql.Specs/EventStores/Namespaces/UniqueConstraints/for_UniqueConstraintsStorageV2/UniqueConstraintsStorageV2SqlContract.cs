// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public abstract partial class UniqueConstraintsStorageV2SqlContract : UniqueConstraintsStorageV2Contract
{
    protected string _connectionString;
    protected int _optionsCreated;
    readonly given.V2Interleaving _interleaving = new();

    protected abstract Task<string> CreateDatabase();

    protected abstract DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString);

    protected abstract Task ReleaseDatabase();

    protected override async Task<IUniqueConstraintsStorageV2> CreateStorage()
    {
        _connectionString = await CreateDatabase();
        return Handle();
    }

    protected IUniqueConstraintsStorageV2 Handle(string sequence = "log", string? connectionString = null) => new UniqueConstraintsStorageV2(sequence, () =>
    {
        Interlocked.Increment(ref _optionsCreated);
        return Configure(new DbContextOptionsBuilder(), connectionString ?? _connectionString).AddInterceptors(_interleaving, _interleaving.Commands).Options;
    });

    protected override Task<IUniqueConstraintsStorageV2> ReopenStorage() => Task.FromResult(Handle());

    protected override Task DestroyStorage() => ReleaseDatabase();

    protected override Task<Result<bool, UniqueConstraintWriteFailure>> Interleave(Func<Task> transition, Func<Task<Result<bool, UniqueConstraintWriteFailure>>> mutation, bool transitionFirst) => _interleaving.Run(transition, mutation, transitionFirst);
}
