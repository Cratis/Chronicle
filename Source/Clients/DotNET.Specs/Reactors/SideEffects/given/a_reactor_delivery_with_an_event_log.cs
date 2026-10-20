// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.SideEffects.given;

public class a_reactor_delivery_with_an_event_log : Specification
{
    protected IEventLog _eventLog;
    protected IEventStore _eventStore;
    protected ILogger _logger;
    protected ReactorDelivery _delivery;
    protected ReactorContext _reactorContext;
    protected EventSourceId _eventSourceId;
    protected ReactorId _reactorId;

    /// <summary>
    /// Gets whether the connected kernel advertises idempotent appends.
    /// </summary>
    protected virtual bool KernelSupportsIdempotentAppends => true;

    void Establish()
    {
        _reactorId = $"reactor-{Guid.NewGuid():N}";
        _eventSourceId = EventSourceId.New();
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.SupportsIdempotentAppends.Returns(KernelSupportsIdempotentAppends);
        _eventLog.AppendMany(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]));
        _eventLog.AppendMany(default!, default, default, default)
            .ReturnsForAnyArgs(AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]));
        _eventLog.Append(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        _eventLog.AppendOnce(default!, default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _delivery = DeliveryAt(7UL);
        _reactorContext = ContextFor(_delivery);
    }

    protected ReactorDelivery DeliveryAt(EventSequenceNumber sequenceNumber) =>
        new(_reactorId, "orders", "default", EventSequenceId.Log, _eventSourceId, sequenceNumber);

    protected ReactorContext ContextFor(ReactorDelivery delivery) =>
        new(EventContext.EmptyWithEventSourceId(_eventSourceId), new object(), ReactorContextValues.Empty)
        {
            Delivery = delivery,
            Logger = _logger
        };

    protected int WarningsLogged => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log));

    protected IEnumerable<EventForEventSourceId> KeyedBatches() =>
        _eventLog.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IEventSequence.AppendMany) && call.GetArguments()[0] is IEnumerable<EventForEventSourceId>)
            .SelectMany(call => (IEnumerable<EventForEventSourceId>)call.GetArguments()[0]!);

    protected int UnkeyedAppendManyCalls => _eventLog.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.AppendMany) && call.GetArguments()[0] is EventSourceId);
}
