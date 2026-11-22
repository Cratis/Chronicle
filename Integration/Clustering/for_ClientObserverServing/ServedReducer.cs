// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

public class ServedReducer(ServedSignal signal, ILocalSiloDetails localSiloDetails) : IReducerFor<ServedReadModel>
{
    public Task<ServedReadModel?> OnServedEvent(ServedEvent @event, ServedReadModel? current, EventContext eventContext)
    {
        signal.RecordReduced(localSiloDetails.SiloAddress.ToParsableString(), eventContext.EventSourceId, @event.Sequence);
        var previous = current ?? new ServedReadModel(0, 0, true);
        return Task.FromResult<ServedReadModel?>(new(previous.Count + 1, @event.Sequence, previous.InOrder && @event.Sequence > previous.Last));
    }
}
