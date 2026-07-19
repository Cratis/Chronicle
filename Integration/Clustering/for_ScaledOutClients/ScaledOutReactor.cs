// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

public class ScaledOutReactor(ScaledOutReactorSignal signal, ILocalSiloDetails localSiloDetails) : IReactor
{
    public Task OnScaledWorkPerformed(ScaledWorkPerformed @event, EventContext eventContext)
    {
        signal.RecordHandled(localSiloDetails.SiloAddress.ToParsableString(), eventContext.EventSourceId);
        return Task.CompletedTask;
    }
}
