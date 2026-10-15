// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers.when_generation_is_pinned;

[DependencyInjection.IgnoreConvention]
public class PinnedReducer(
    TaskCompletionSource<(PinnedPersonRegistered Event, EventContext Context)> live,
    TaskCompletionSource<(PinnedPersonRegistered Event, EventContext Context)> replay) : IReducerFor<PinnedReadModel>
{
    public Task<PinnedReadModel> OnRegistered(PinnedPersonRegistered @event, PinnedReadModel? current, EventContext context)
    {
        if (context.ObservationState.HasFlag(EventObservationState.Replay))
        {
            replay.TrySetResult((@event, context));
        }
        else
        {
            live.TrySetResult((@event, context));
        }

        return Task.FromResult(new PinnedReadModel(@event.FullName));
    }
}
