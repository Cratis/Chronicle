// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_from_all;

public class AllEventsProjection : IProjectionFor<AllEvents>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<AllEvents> builder) => builder
        .From<ThingOpened>(_ => _.Set(model => model.Name).To(@event => @event.Name))
        .FromAll(_ => _.Count(model => model.EventCount));
}
