// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

public class ItemsProjection : IProjectionFor<ItemsReadModel>
{
    public void Define(IProjectionBuilderFor<ItemsReadModel> builder) => builder
        .NoAutoMap()
        .From<ItemAdded>(_ => _.Count(m => m.TotalCount));
}
