// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Grains.Observation.for_Observer;

public class ObserverSubscriber : IObserverSubscriber
{
    public Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) => throw new NotImplementedException();
}
