// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key;

public class EventSourceAccountSummaryProjection : IProjectionFor<EventSourceAccountSummary>
{
    public void Define(IProjectionBuilderFor<EventSourceAccountSummary> builder) => builder
        .From<AccountOpened>()
        .Join<CompanyNamed>(join => join
            .On(model => model.CompanyId)
            .Set(model => model.CompanyName).To(@event => @event.Name));
}
