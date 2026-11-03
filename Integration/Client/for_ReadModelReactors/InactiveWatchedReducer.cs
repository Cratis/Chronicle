// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
[Reducer("34180000-0000-4000-8000-000000000005", isActive: false)]
public class InactiveWatchedReducer : IReducerFor<ReducerWatchedReadModel>
{
    public ReducerWatchedReadModel On(ReducerWatchedEvent @event, ReducerWatchedReadModel? current) => new(@event.Value, @event.Marker);
}
