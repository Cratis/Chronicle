// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ClusteredReducer : IReducerFor<ClusteredReducerReadModel>
{
    public Task<ClusteredReducerReadModel?> OnClusteredEvent(ClusteredEvent @event, ClusteredReducerReadModel? current, EventContext eventContext) =>
        Task.FromResult<ClusteredReducerReadModel?>(new(@event.Number, @event.Reference, @event.Priority, @event.Tags, @event.Location));
}
