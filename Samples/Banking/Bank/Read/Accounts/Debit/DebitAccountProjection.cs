// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.Owner).To(@event => @event.Owner)
                .Set(model => model.LastUpdated).ToEventContextProperty(context => context.Occurred)
                .Set(model => model.HasCard).To(@event => @event.IncludeCard))
            .From<DebitAccountNameChanged>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.LastUpdated).ToEventContextProperty(context => context.Occurred))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount)
                .Set(model => model.LastUpdated).ToEventContextProperty(context => context.Occurred))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount)
                .Set(model => model.LastUpdated).ToEventContextProperty(context => context.Occurred))
            .RemovedWith<DebitAccountClosed>();
}
