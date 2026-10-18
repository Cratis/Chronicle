// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_receipted_unit : a_unit_of_work
{
    protected IChronicleConnection _connection;
    protected IOccurrences _occurrences;
    protected EffectKey _key;
    protected EffectReceiptResult _receiptResult;
    protected Contracts.Occurrences.EffectReceipt _sentReceipt;
    protected int _receiptedAppends;
    private protected ReadProtectionCommit _readProtection;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IKernelCapabilities>();
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.EffectReceipts]);
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(true);
        _eventStore.Connection.Returns(_connection);
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _key = new("occurrence", "send", new("occurrence", 1));
        _receiptResult = new(EffectReceiptStatus.Recorded, new(_key, new NoEvents(), null, _key.WrittenBy, DateTimeOffset.UtcNow));
        _occurrences = Substitute.For<IOccurrences, IOccurrencesForUnitOfWork>();
        _eventStore.Occurrences.Returns(_occurrences);
        _occurrences.RecordEffectReceipt(Arg.Any<EffectKey>(), Arg.Any<EffectOutcome>(), Arg.Any<System.Text.Json.Nodes.JsonNode>()).Returns(_ => _receiptResult);
        ((IOccurrencesForUnitOfWork)_occurrences).RecordNoEvents(Arg.Any<EffectKey>(), Arg.Any<System.Text.Json.Nodes.JsonNode>()).Returns(_ => _receiptResult);
        _eventSequence = Substitute.For<IEventSequence, IEventSequenceWithEffectReceipts>();
        _eventStore.GetEventSequence(EventSequenceId.Log).Returns(_eventSequence);
        ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>(),
            Arg.Any<Contracts.Occurrences.EffectReceipt>(),
            Arg.Any<ReadProtectionCommit>()).Returns(call =>
            {
                _receiptedAppends++;
                _eventsAppended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                _concurrencyScopesAppended = call.Arg<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>().ToDictionary(_ => _.Key, _ => _.Value);
                _sentReceipt = call.Arg<Contracts.Occurrences.EffectReceipt>();
                _readProtection = call.Arg<ReadProtectionCommit>();
                return _appendResult with { EffectReceipt = _appendResult.IsSuccess ? _receiptResult : null };
            });
    }
}
