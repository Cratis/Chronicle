// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.given;

public class a_resolver : Specification
{
    protected ReactorMethodArgumentsResolver _resolver;
    protected IEventStore _eventStore;
    protected IProjections _projections;
    protected IReducers _reducers;
    protected IReadModels _readModels;
    protected IServiceProvider _serviceProvider;
    protected EventContext _eventContext;
    protected EventSourceId _eventSourceId;
    protected SomeEvent _event;

    void Establish()
    {
        _resolver = new ReactorMethodArgumentsResolver();
        _projections = Substitute.For<IProjections>();
        _reducers = Substitute.For<IReducers>();
        _readModels = Substitute.For<IReadModels>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Projections.Returns(_projections);
        _eventStore.Reducers.Returns(_reducers);
        _eventStore.ReadModels.Returns(_readModels);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _eventSourceId = EventSourceId.New();
        _eventContext = EventContext.EmptyWithEventSourceId(_eventSourceId);
        _event = new SomeEvent();
    }

    protected MethodInfo MethodNamed(string name) => typeof(ReactorWithDependencies).GetMethod(name)!;
}
