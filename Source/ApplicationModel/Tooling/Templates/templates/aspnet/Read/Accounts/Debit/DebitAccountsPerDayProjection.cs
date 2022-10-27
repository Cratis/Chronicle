// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

public class DebitAccountsPerDayProjection : IProjectionFor<DebitAccountsPerDay>
{
    public ProjectionId Identifier => "ba78bf07-0a4c-4e05-b505-cc516a33bd80";

    public void Define(IProjectionBuilderFor<DebitAccountsPerDay> builder) => builder
        .From<DebitAccountOpened>(_ => _
            .UsingCompositeKey<DebitAccountsPerDayKey>(key => key
                .Set(k => k.Day).ToEventContextProperty(e => e.Occurred.Day)
                .Set(k => k.Month).ToEventContextProperty(e => e.Occurred.Month)
                .Set(k => k.Year).ToEventContextProperty(e => e.Occurred.Year))
            .Count(m => m.Count));
}
