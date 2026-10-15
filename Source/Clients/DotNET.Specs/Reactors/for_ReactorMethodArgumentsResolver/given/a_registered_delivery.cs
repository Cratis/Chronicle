// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.given;

public class a_registered_delivery<TReactor> : for_Reactors.given.all_dependencies
    where TReactor : IReactor, new()
{
    protected ReactorDelivery _delivery;
    protected ReactorDelivery _redelivery;
    protected ReactorDelivery _expected;
    protected ReactorDelivery _legacyDelivery;
    protected ReactorDelivery _fallbackDelivery;
    protected IReactorHandler _handler;
    protected EventContext _context;
    protected virtual EventStoreName CurrentStore => "current";

    void Establish()
    {
        var identity = new EventType("registered-external-event", 3);
        var registry = RegisteredEventTypesForSpecifications.Create((typeof(ExternalEvent), identity));
        _eventStore.EventTypes.Returns(registry);
        _eventStore.Name.Returns(CurrentStore);
        _clientArtifactsProvider.Reactors.Returns([typeof(TReactor)]);
        _context = EventContext.From(CurrentStore, "tenant", identity, EventSourceType.Default, "partition", EventStreamType.All, EventStreamId.Default, 42UL, CorrelationId.NotSet);
    }

    protected async Task ResolveDeliveries()
    {
        await _reactors.Discover();
        _handler = _reactors.Handlers.Single();
        var reactor = new TReactor();
        var method = typeof(TReactor).GetMethod("Handle")!;
        var resolver = new ReactorMethodArgumentsResolver();
        _legacyDelivery = ReactorDelivery.For(reactor, _context);
        var arguments = await resolver.Resolve(method, reactor, new ExternalEvent(), _context, _eventStore, null);
        var replayArguments = await resolver.Resolve(method, reactor, new ExternalEvent(), _context with { ObservationState = EventObservationState.Replay }, _eventStore, null);
        var fallbackArguments = await resolver.Resolve(method, reactor, new ExternalEvent(), _context, null, null);
        _fallbackDelivery = (ReactorDelivery)fallbackArguments[1]!;
        _delivery = (ReactorDelivery)arguments[1]!;
        _redelivery = (ReactorDelivery)replayArguments[1]!;
        _expected = ReactorDelivery.For(_handler.Id, _handler.EventSequenceId, _context);
    }
}
