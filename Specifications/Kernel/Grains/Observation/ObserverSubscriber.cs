// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor;

public class ObserverSubscriber : IObserverSubscriber
{
    public Task<ObserverSubscriberResult> OnNext(AppendedEvent @event)
    {
        throw new NotImplementedException();
    }
}
