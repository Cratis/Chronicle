// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

public class ServedReactor(ServedSignal signal, ILocalSiloDetails localSiloDetails) : IReactor
{
    public Task OnServedEvent(ServedEvent @event, EventContext eventContext)
    {
        signal.RecordReacted(localSiloDetails.SiloAddress.ToParsableString(), eventContext.EventSourceId, @event.Sequence);
        return Task.CompletedTask;
    }
}
