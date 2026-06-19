// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorInvoker.given;

public class an_invoker : Specification
{
    protected ReadModelReactorInvoker _invoker;
    protected IReactorSideEffectHandlers _sideEffectHandlers;
    protected IReactorContextValuesBuilder _contextValuesBuilder;
    protected IEventStore _eventStore;
    protected IServiceProvider _serviceProvider;
    protected EventContext _changeContext;
    protected RecordingReactor _reactor;

    void Establish()
    {
        _sideEffectHandlers = Substitute.For<IReactorSideEffectHandlers>();
        _contextValuesBuilder = Substitute.For<IReactorContextValuesBuilder>();
        _contextValuesBuilder.Build(Arg.Any<object>(), Arg.Any<EventContext>()).Returns(ReactorContextValues.Empty);
        _eventStore = Substitute.For<IEventStore>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _changeContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
        _reactor = new RecordingReactor();
        _invoker = new ReadModelReactorInvoker(_sideEffectHandlers, _contextValuesBuilder, Substitute.For<ILogger<ReadModelReactorInvoker>>());
    }

    protected ReadModelReactorMethod MethodFor(ReadModelChangeType changeType) =>
        ReadModelReactorMethods.GetFor(typeof(RecordingReactor)).Single(_ => _.ChangeType == changeType);
}
