// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.given;

public class an_reactor_invoker_for<TReactor> : Specification
{
    protected IServiceProvider _serviceProvider;
    protected IEventTypes _eventTypes;
    protected IReactorMiddlewares _middlewares;
    protected IClientArtifactsActivator _artifactActivator;
    protected ReactorInvoker _invoker;

    void Establish()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _eventTypes = new EventTypesForSpecifications(GetEventTypes());
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();

        _invoker = new ReactorInvoker(
            _eventTypes,
            _middlewares,
            _artifactActivator,
            typeof(TReactor),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
