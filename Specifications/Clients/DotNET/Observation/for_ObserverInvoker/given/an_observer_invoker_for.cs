// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverInvoker.given;

public class an_observer_invoker_for<TObserver> : Specification
{
    protected Mock<IServiceProvider> service_provider;
    protected IEventTypes event_types;
    protected Mock<IObserverMiddlewares> middlewares;
    protected ObserverInvoker invoker;

    void Establish()
    {
        service_provider = new();
        event_types = new EventTypesForSpecifications(GetEventTypes());
        middlewares = new();

        invoker = new ObserverInvoker(
            service_provider.Object,
            event_types,
            middlewares.Object,
            typeof(TObserver),
            Mock.Of<ILogger<ObserverInvoker>>());
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
