// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ClusteredReactor(ClusteredReactorSignal signal) : IReactor
{
    public Task OnClusteredEvent(ClusteredEvent @event, EventContext eventContext)
    {
        signal.RecordHandled(@event.Reference.Value);
        return Task.CompletedTask;
    }
}
