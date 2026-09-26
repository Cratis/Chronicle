// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

public class FluentJoinedAllEventsProjection : IProjectionFor<FluentJoinedAllEvents>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentJoinedAllEvents> builder) => builder
        .From<JoinOrderPlaced>(_ => _.Set(model => model.CustomerId).To(@event => @event.CustomerId))
        .Join<JoinCustomerRegistered>(_ => _.On(model => model.CustomerId).Set(model => model.CustomerName).To(@event => @event.Name))
        .FromAll(_ => _.Count(model => model.EventCount));
}
