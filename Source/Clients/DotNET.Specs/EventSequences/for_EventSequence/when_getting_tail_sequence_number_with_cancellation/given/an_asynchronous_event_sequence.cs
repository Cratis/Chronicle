// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation.given;

public class an_asynchronous_event_sequence : for_EventSequence.given.an_event_sequence
{
    protected async_connection _asynchronous;
    protected IChronicleServicesAccessor _accessor;
    protected CancellationTokenSource _cancellation;
    protected CancellationToken _acquisitionToken;
    protected CancellationToken _rpcToken;
    protected Contracts.Sequences.TailSequenceNumberRequest _request;
    protected int _rpcCalls;

    void Establish()
    {
        _cancellation = new();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _accessor = (IChronicleServicesAccessor)_connection;
        _accessor.Services.Returns(services);
        _asynchronous = new(_connection, _accessor)
        {
            Acquire = token =>
            {
                _acquisitionToken = token;
                return ValueTask.FromResult(services);
            }
        };
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.TailSequenceNumberRequest>();
            _rpcToken = call.Arg<CallContext>().CancellationToken;
            _rpcCalls++;
            return QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 75UL });
        });
        _eventSequence = new(
            "tail-store",
            "tail-namespace",
            "tail-sequence",
            _asynchronous,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default);
    }

    void Destroy() => _cancellation.Dispose();
}
