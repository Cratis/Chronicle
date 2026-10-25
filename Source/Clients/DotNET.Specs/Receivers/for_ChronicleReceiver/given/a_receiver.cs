// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.given;

public class a_receiver : Specification
{
    protected IChronicleClient _client;
    protected IEventStore _store;
    protected IEventStoreNamespaceResolver _namespaceResolver;
    private protected IDeliveryDecoder _decoder;
    private protected IReceiveReactorDeliveries _reactors;
    private protected IReceiveReducerDeliveries _reducers;
    private protected ChronicleReceiver _receiver;
    private protected DeliveryOutcome _outcome;
    private protected Delivery _delivery;
    protected MemoryStream _body;
    private protected readonly DeliveryTarget _target = new(Observation.ObserverType.Reactor, "handler", "store", "tenant");

    void Establish()
    {
        _body = new();
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(new ChronicleClientOptions { EventStore = "store" });
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        _namespaceResolver.Resolve().Returns((EventStoreNamespaceName)"tenant");
        _store = Substitute.For<IEventStore>();
        _decoder = Substitute.For<IDeliveryDecoder>();
        _delivery = new(
            _target,
            "order-42",
            [new()
            {
                Context = (EventContext.Empty with { SequenceNumber = 12, EventType = new("orders", 2) }).ToContract(),
                Content = "{\"order\":42}"
            }],
            null);
        _decoder.Decode(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>()).Returns(_ => _delivery);
        var reactors = Substitute.For<Cratis.Chronicle.Reactors.IReactors, IReceiveReactorDeliveries>();
        var reducers = Substitute.For<Cratis.Chronicle.Reducers.IReducers, IReceiveReducerDeliveries>();
        _reactors = (IReceiveReactorDeliveries)reactors;
        _reducers = (IReceiveReducerDeliveries)reducers;
        _store.Reactors.Returns(reactors);
        _store.Reducers.Returns(reducers);
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_store);
        _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns(new ReactorResult
        {
            Partition = "order-42", State = Contracts.Observation.ObservationState.Success, LastSuccessfulObservation = 12, ExceptionStackTrace = string.Empty
        });
        _reducers.Deliver(Arg.Any<ReducerId>(), Arg.Any<ReduceOperationMessage>()).Returns(new ReducerResult
        {
            Partition = "order-42", State = Contracts.Observation.ObservationState.Success, LastSuccessfulObservation = 12, ExceptionStackTrace = string.Empty, ReadModelState = "{\"count\":4}"
        });
        _receiver = new(_client, _decoder, NullLogger<ChronicleReceiver>.Instance, new(_client, _namespaceResolver));
    }

    private protected async Task Receive(CancellationToken cancellationToken = default) =>
        _outcome = await _receiver.Receive(new(_body, _delivery.Target) { SignatureVerified = true }, cancellationToken);

    void Destroy() => _body?.Dispose();
}
