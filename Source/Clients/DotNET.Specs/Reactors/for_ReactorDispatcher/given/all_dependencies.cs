// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IActivateReactorMiddlewares _middlewaresActivator;
    protected IEventSerializer _eventSerializer;
    protected IActivitySource<Reactors> _activitySource;
    protected IReactorSideEffectHandlers _sideEffectHandlers;
    protected IReactorContextValuesBuilder _reactorContextValuesBuilder;
    protected IReactorMethodArgumentsResolver _argumentsResolver;
    protected ILogger<Reactors> _logger;
    protected ICausationManager _causationManager;
    protected IIdentityProvider _identityProvider;
    protected List<ReactorResult> _published;
    System.Diagnostics.ActivitySource _traceSource;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"ns");
        _eventTypes = Substitute.For<IEventTypes>();
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _middlewaresActivator = Substitute.For<IActivateReactorMiddlewares>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _traceSource = new("reactor-dispatcher-specification");
        _activitySource = new ActivitySource<Reactors>(_traceSource);
        _sideEffectHandlers = Substitute.For<IReactorSideEffectHandlers>();
        _reactorContextValuesBuilder = Substitute.For<IReactorContextValuesBuilder>();
        _argumentsResolver = Substitute.For<IReactorMethodArgumentsResolver>();
        _logger = Substitute.For<ILogger<Reactors>>();
        _causationManager = Substitute.For<ICausationManager>();
        _identityProvider = Substitute.For<IIdentityProvider>();
        _published = [];
    }

    void Destroy() => _traceSource.Dispose();

    protected static ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

    protected void Publish(ReactorResult result) => _published.Add(result);

    protected static EventsToObserve EventsWith(params ulong[] sequenceNumbers) => new()
    {
        Partition = "partition",
        DeliveryId = 42,
        Events = [.. sequenceNumbers.Select(number => new Contracts.Events.AppendedEvent
        {
            Context = (EventContext.Empty with { EventType = new("something-happened", 1), SequenceNumber = number }).ToContract(),
            Content = "{\"name\":\"event\"}",
            GenerationalContent = new Dictionary<int, string>()
        })]
    };
}
