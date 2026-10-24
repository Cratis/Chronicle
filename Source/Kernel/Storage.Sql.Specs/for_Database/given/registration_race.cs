// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Reactors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

using KernelReactorDefinition = Cratis.Chronicle.Concepts.Observation.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public static class registration_race
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    public static async Task<(int Count, bool IsReplayable)> Run(Database database, bool differingPayload = false)
    {
        await using var scope = await database.EventStore("registration-race");
        var options = (DbContextOptions<EventStoreDbContext>)scope.DbContext.GetService<IDbContextOptions>();
        var firstGate = new command_gate();
        var secondGate = new command_gate();
        await using var first = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>(options).AddInterceptors(firstGate).Options);
        await using var second = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>(options).AddInterceptors(secondGate).Options);
        var connections = Substitute.For<IDatabase>();
        connections.EventStore("registration-race").Returns(
            Task.FromResult(new DbContextScope<EventStoreDbContext>(first, () => { })),
            Task.FromResult(new DbContextScope<EventStoreDbContext>(second, () => { })));
        var storage = new ReactorDefinitionsStorage("registration-race", connections);
        var initial = new KernelReactorDefinition("same-id", ReactorOwner.Client, "event-log", [], IsReplayable: true);
        var firstSave = storage.Save(initial);
        var secondSave = storage.Save(differingPayload ? initial with { IsReplayable = false } : initial);
        try
        {
            await Task.WhenAll(firstGate.Arrived.Task, secondGate.Arrived.Task).WaitAsync(_deadline);
            firstGate.Release.TrySetResult();
            await firstSave.WaitAsync(_deadline);
            secondGate.Release.TrySetResult();
            await secondSave.WaitAsync(_deadline);
        }
        finally
        {
            firstGate.Release.TrySetResult();
            secondGate.Release.TrySetResult();
            await Task.WhenAll(firstSave, secondSave).WaitAsync(_deadline);
        }

        var saved = await scope.DbContext.Reactors.AsNoTracking().ToArrayAsync();
        return (saved.Length, saved.Single().IsReplayable);
    }

    sealed class command_gate : DbCommandInterceptor
    {
        internal TaskCompletionSource Arrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await WaitForRelease(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await WaitForRelease(command, cancellationToken);
            return result;
        }

        async Task WaitForRelease(DbCommand command, CancellationToken cancellationToken)
        {
            if (command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                command.CommandText.Contains("MERGE", StringComparison.OrdinalIgnoreCase))
            {
                Arrived.TrySetResult();
                await Release.Task.WaitAsync(_deadline, cancellationToken);
            }
        }
    }
}
