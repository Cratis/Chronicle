// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.given;

public class an_reactor_invoker_for<TReactor> : Specification
{
    protected Mock<IServiceProvider> service_provider;
    protected IEventTypes event_types;
    protected Mock<IReactorMiddlewares> middlewares;
    protected ReactorInvoker invoker;

    void Establish()
    {
        service_provider = new();
        event_types = new EventTypesForSpecifications(GetEventTypes());
        middlewares = new();

        invoker = new ReactorInvoker(
            event_types,
            middlewares.Object,
            typeof(TReactor),
            Mock.Of<ILogger<ReactorInvoker>>());
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
