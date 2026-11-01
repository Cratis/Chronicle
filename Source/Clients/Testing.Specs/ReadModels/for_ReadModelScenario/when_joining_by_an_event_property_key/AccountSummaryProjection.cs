// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key;

public class AccountSummaryProjection : IProjectionFor<AccountSummary>
{
    public void Define(IProjectionBuilderFor<AccountSummary> builder) => builder
        .From<AccountOpened>()
        .Join<CompanyNamed>(join => join
            .On(model => model.CompanyId)
            .UsingKey(@event => @event.CompanyId)
            .Set(model => model.CompanyName).To(@event => @event.Name));
}
