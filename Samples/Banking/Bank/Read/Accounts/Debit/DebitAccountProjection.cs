// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;
using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .WithInitialValues(() => new(Guid.Empty, string.Empty, null!, new(string.Empty, string.Empty), 0, false, DateTimeOffset.MinValue))
            .FromEvery(_ => _
                .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
                .IncludeChildProjections())
            .Join<AccountHolderRegistered>(_ => _
                .On(model => model.AccountHolderId)
                .Set(model => model.AccountHolder.FirstName).To(@event => @event.FirstName)
                .Set(model => model.AccountHolder.LastName).To(@event => @event.LastName))
            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.AccountHolderId).To(@event => @event.Owner)
                .Set(model => model.HasCard).To(@event => @event.IncludeCard))
            .From<DebitAccountNameChanged>(_ => _
                .Set(model => model.Name).To(@event => @event.Name))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount))
            .RemovedWith<DebitAccountClosed>();
}
