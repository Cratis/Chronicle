// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

public class PotentialMoneyLaundryCaseProjection : IProjectionFor<PotentialMoneyLaundryCase>
{
    public ProjectionId Identifier => "bc804f6f-dea6-49e4-8747-e8ae958e1ba9";

    public void Define(IProjectionBuilderFor<PotentialMoneyLaundryCase> builder) => builder
        .From<PossibleMoneyLaunderingDetected>(_ => _
            .Set(model => model.PersonId).To(@event => @event.PersonId)
            .Set(model => model.AccountId).To(@event => @event.AccountId)
            .Set(model => model.LastOccurrence).To(@event => @event.LastOccurrence));
}
