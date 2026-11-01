// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_transitioning;

public class and_an_enlisted_transaction_is_rolled_back : given.a_sqlite_ledger
{
    ReadModelTableNameBinding _stored;
    bool _moved;

    async Task Establish() => await Reserve(_owner, "orders");

    async Task Because()
    {
        await using var model = CreateContext();
        await using (var transaction = await model.Database.BeginTransactionAsync())
        {
            await using var enlisted = await ReadModelTableBindingsDbContext.Enlist(model, "main");
            _moved = await CreateLedger(enlisted).Transition(_owner.Id, ReadModelTableBindingState.Reserved, ReadModelTableBindingState.Ready);
            await transaction.RollbackAsync();
        }

        _stored = await CreateLedger(CreateContext()).Find(_owner.Id);
    }

    [Fact] void should_have_changed_the_row_inside_the_transaction() => _moved.ShouldBeTrue();
    [Fact] void should_roll_the_ledger_back_with_the_transaction() => _stored.State.ShouldEqual(ReadModelTableBindingState.Reserved);
}
