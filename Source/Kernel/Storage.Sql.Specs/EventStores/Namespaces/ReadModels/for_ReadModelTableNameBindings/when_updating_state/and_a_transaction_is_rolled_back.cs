// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_updating_state;

public class and_a_transaction_is_rolled_back : given.a_sqlite_ledger
{
    ReadModelTableNameBinding _stored;

    async Task Establish()
    {
        _owner = _owner with { Role = ReadModelTableRole.Replay, LogicalName = "replay-orders" };
        await Reserve(_owner, "replay-orders");
        var row = await _context.Bindings.SingleAsync();
        row.State = ReadModelTableBindingState.Ready;
        row.ReplayAttempt = "orders-20260607120000-12345678";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    async Task Because()
    {
        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            var row = await _context.Bindings.SingleAsync();
            row.State = ReadModelTableBindingState.Removed;
            row.ReplayAttempt = null;
            await _context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        _stored = await CreateLedger(CreateContext()).Find(_owner.Id);
    }

    [Fact] void should_preserve_the_committed_state() => _stored.State.ShouldEqual(ReadModelTableBindingState.Ready);
    [Fact] void should_preserve_the_full_replay_attempt() => _stored.ReplayAttempt.ShouldEqual("orders-20260607120000-12345678");
    [Fact] void should_preserve_the_immutable_name() => _stored.PhysicalName.ShouldEqual("replay-orders");
}
