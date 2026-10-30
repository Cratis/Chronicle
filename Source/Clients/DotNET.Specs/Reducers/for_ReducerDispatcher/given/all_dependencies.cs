// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IEventTypes _eventTypes;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected IIdentityProvider _identityProvider;
    protected IActivitySource<Reducers> _activitySource;
    protected ILogger<Reducers> _logger;
    protected IReducerHandler _handler;
    protected List<ReducerResult> _published;
    System.Diagnostics.ActivitySource _traceSource;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"ns");

        _serviceProvider = Substitute.For<IServiceProvider>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(SomethingHappened));
        _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        _identityProvider = Substitute.For<IIdentityProvider>();
        _traceSource = new("reducer-dispatcher-specification");
        _activitySource = new ActivitySource<Reducers>(_traceSource);
        _logger = Substitute.For<ILogger<Reducers>>();

        _handler = Substitute.For<IReducerHandler>();
        _handler.Id.Returns((ReducerId)"reducer");
        _handler.EventSequenceId.Returns(EventSequenceId.Log);
        _handler.ReadModelType.Returns(typeof(State));
        _handler.ReducerType.Returns(typeof(object));
        _handler.EventTypes.Returns([new EventType("something-happened", 1)]);

        _published = [];
    }

    void Destroy() => _traceSource.Dispose();

    protected void Publish(ReducerResult result) => _published.Add(result);

    protected static ReduceOperationMessage OperationWith(params ulong[] sequenceNumbers) => new()
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

    public record SomethingHappened(string Name);

    public record State(string Name);
}
