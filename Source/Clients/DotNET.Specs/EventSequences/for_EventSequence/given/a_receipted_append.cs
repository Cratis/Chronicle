// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_receipted_append : all_dependencies
{
    protected EventSequence _eventSequence;
    protected EffectKey _key;
    protected Contracts.Occurrences.EffectReceipt _receipt;
    protected Contracts.Sequences.AppendManyForEventSourcesRequest _request;
    protected Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest _taggedRequest;
    protected Contracts.Sequences.AppendManyResponse _response;
    protected AppendManyResult _result;
    protected Dictionary<EventSourceId, ConcurrencyScope> _scopes = [];
    protected int _notifications;
    IDisposable _subscription;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IChronicleServicesAccessor)_connection).Services.Returns(services);
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(true);
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.EffectReceipts]);
        _eventSequence = new("store", "namespace", EventSequenceId.Log, _connection, _eventTypes, _constraints, _eventSerializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _unitOfWorkManager, _identityProvider, JsonSerializerOptions.Default);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("created", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _key = new("occurrence", "send", new("occurrence", 2));
        _receipt = EffectReceiptSupport.ToContract(_key, JsonValue.Create("attempt"));
        _response = new()
        {
            IsSuccess = true,
            SequenceNumbers = [42],
            ConstraintViolations = [],
            ConcurrencyViolations = [],
            Errors = [],
            EffectReceipt = new()
            {
                Id = _key.Value,
                OccurrenceId = _key.Occurrence,
                Epoch = 1,
                Outcome = Contracts.Storage.Occurrences.EffectOutcome.Appended,
                EventSequenceId = EventSequenceId.Log,
                FirstSequenceNumber = 42,
                LastSequenceNumber = 42,
                Reasons = [],
                Response = "\"winner\"",
                RecordedAt = DateTimeOffset.UtcNow
            }
        };
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), _response);
        });
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default).Returns(call =>
        {
            _taggedRequest = call.Arg<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), _response);
        });
        _subscription = _eventSequence.AppendOperations.Subscribe(_ => _notifications++);
    }

    void Destroy() => _subscription.Dispose();
}
