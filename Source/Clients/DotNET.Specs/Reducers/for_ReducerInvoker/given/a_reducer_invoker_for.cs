// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.given;

public class a_reducer_invoker_for<TReducer> : Specification
    where TReducer : class, new()
{
    protected ReducerInvoker _invoker;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IEventTypes _eventTypes;
    protected EventType _eventType;

    void Establish()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(TReducer))
            .Returns(callInfo => new ActivatedArtifact(
                new TReducer(),
                typeof(TReducer),
                NullLoggerFactory.Instance));
        _eventTypes = new EventTypesForSpecifications(GetEventTypes());
        _eventType = new("d22efe41-41c6-408e-b5d2-c0d54757cbf8", 1);
        _invoker = new ReducerInvoker(
            _eventTypes,
            _artifactActivator,
            typeof(TReducer),
            typeof(ReadModel),
            nameof(ReadModel));
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
