// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

public class FluentAllEventsProjection : IProjectionFor<FluentAllEvents>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentAllEvents> builder) => builder
        .From<FluentThingOpened>(_ => _.Set(model => model.Name).To(@event => @event.Name))
        .FromAll(_ => _.Count(model => model.EventCount));
}
