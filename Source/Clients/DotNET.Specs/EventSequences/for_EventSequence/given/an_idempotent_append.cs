// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_idempotent_append : all_dependencies
{
    protected EventSequence _eventSequence;
    protected IdempotencyKey _key;
    protected Contracts.Sequences.AppendOnceRequest _singleRequest;
    protected Contracts.Sequences.AppendOnceWithNamedTagsRequest _singleTaggedRequest;
    protected Contracts.Sequences.AppendManyOnceRequest _batchRequest;
    protected Contracts.Sequences.AppendManyOnceWithNamedTagsRequest _batchTaggedRequest;
    protected Contracts.Sequences.AppendOnceResponse _singleResponse;
    protected Contracts.Sequences.AppendManyOnceResponse _batchResponse;
    protected int _notifications;
    protected bool _capabilityAdvertised = true;
    protected bool _capabilitiesKnown = true;
    IDisposable _subscription;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IChronicleServicesAccessor)_connection).Services.Returns(services);
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(_ => _capabilitiesKnown);
        ((IKernelCapabilities)_connection).Capabilities.Returns(_ => _capabilityAdvertised ? [KernelCapabilities.IdempotentAppends] : []);
        _eventSequence = new("store", "namespace", EventSequenceId.Log, _connection, _eventTypes, _constraints, _eventSerializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _unitOfWorkManager, _identityProvider, JsonSerializerOptions.Default);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("created", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _key = new("order-1");
        _singleResponse = new() { IsSuccess = true, SequenceNumber = 42, Outcome = Contracts.EventSequences.AppendOutcome.Committed };
        _batchResponse = new()
        {
            IsSuccess = true,
            SequenceNumbers = [1, 2],
            Outcomes = [Contracts.EventSequences.AppendOutcome.Committed, Contracts.EventSequences.AppendOutcome.AlreadyAppended]
        };
        _sequences.AppendOnce(Arg.Any<Contracts.Sequences.AppendOnceRequest>(), CallContext.Default).Returns(call =>
        {
            _singleRequest = call.Arg<Contracts.Sequences.AppendOnceRequest>();
            return CommandResult<Contracts.Sequences.AppendOnceResponse>.Success(Guid.NewGuid(), _singleResponse);
        });
        _sequences.AppendOnceWithNamedTags(Arg.Any<Contracts.Sequences.AppendOnceWithNamedTagsRequest>(), CallContext.Default).Returns(call =>
        {
            _singleTaggedRequest = call.Arg<Contracts.Sequences.AppendOnceWithNamedTagsRequest>();
            return CommandResult<Contracts.Sequences.AppendOnceResponse>.Success(Guid.NewGuid(), _singleResponse);
        });
        _sequences.AppendManyOnce(Arg.Any<Contracts.Sequences.AppendManyOnceRequest>(), CallContext.Default).Returns(call =>
        {
            _batchRequest = call.Arg<Contracts.Sequences.AppendManyOnceRequest>();
            return CommandResult<Contracts.Sequences.AppendManyOnceResponse>.Success(Guid.NewGuid(), _batchResponse);
        });
        _sequences.AppendManyOnceWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyOnceWithNamedTagsRequest>(), CallContext.Default).Returns(call =>
        {
            _batchTaggedRequest = call.Arg<Contracts.Sequences.AppendManyOnceWithNamedTagsRequest>();
            return CommandResult<Contracts.Sequences.AppendManyOnceResponse>.Success(Guid.NewGuid(), _batchResponse);
        });
        _subscription = _eventSequence.AppendOperations.Subscribe(_ => _notifications += _.Count());
    }

    void Destroy() => _subscription.Dispose();
}
