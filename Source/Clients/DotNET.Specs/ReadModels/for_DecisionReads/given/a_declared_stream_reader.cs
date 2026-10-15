// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSources;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.given;

public class a_declared_stream_reader : a_decision_reader
{
    [EventSource("BankAccount")]
    [EventSources.EventStream("Transactions")]
    protected class BankAccount : IEventSource;

    [EventStream<BankAccount>("Transactions")]
    protected record MonthlyState(string Id);

    void Establish()
    {
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(BankAccount)).Returns(EventSources.EventSources.Describe(typeof(BankAccount)));
        _store.EventSources.Returns(sources);
        _definition.ReadModel = typeof(MonthlyState).GetReadModelIdentifier();
        _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>()).Returns(new GetDefinitionsResponse
        {
            ReadModels = [new ReadModelDefinition
            {
                Type = new() { Identifier = typeof(MonthlyState).GetReadModelIdentifier() },
                ObserverIdentifier = "projection", ObserverType = ReadModelObserverType.Projection
            }]
        });
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            ++_folds;
            return new GetInstanceByKeyResponse { ReadModel = _json, LastHandledEventSequenceNumber = _last, StreamScoped = true };
        });
    }
}
