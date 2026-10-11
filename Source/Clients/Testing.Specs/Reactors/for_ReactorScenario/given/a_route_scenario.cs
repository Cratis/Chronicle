// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

public class a_route_scenario<TReactor> : Specification
    where TReactor : class, IReactor
{
    protected ReactorScenario<TReactor> _scenario;
    protected IRecordEventRoute _recorder;
    protected EventSourceId _source;
    Defaults _defaults;

    void Establish()
    {
        _source = EventSourceId.New();
        _recorder = Substitute.For<IRecordEventRoute>();
        _defaults = new(new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(ReactorRouteRecorded), typeof(ProducedRouteRecorded)],
            EventSources = [typeof(RoutedOrderSource)]
        });
        _scenario = new(_defaults);
        _scenario.Services.AddSingleton(_recorder);
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
