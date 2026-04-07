// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.EventSequences;
using KernelAppendManyResult = KernelCore::Cratis.Chronicle.EventSequences.AppendManyResult;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;
using KernelConceptsAuditing = KernelConcepts::Cratis.Chronicle.Concepts.Auditing;
using KernelConceptsConcurrency = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using KernelConceptsEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelConceptsIdentities = KernelConcepts::Cratis.Chronicle.Concepts.Identities;
using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-process implementation of <see cref="Contracts.EventSequences.IEventSequences"/> that
/// delegates append operations to the real kernel <see cref="KernelEventSequences::EventSequence"/> grain.
/// </summary>
/// <remarks>
/// <para>
/// This service mirrors the shape of the kernel <c>EventSequences</c> gRPC service but runs entirely
/// in-process without a running Chronicle server. All type conversions between the contract layer
/// and the kernel concept layer are performed locally.
/// </para>
/// <para>
/// Only <see cref="Append"/> and <see cref="AppendMany"/> are fully implemented; all other methods throw
/// <see cref="NotSupportedException"/>.
/// </para>
/// </remarks>
/// <param name="grainFactory">The <see cref="InProcessGrainFactory"/> that provides the kernel event-sequence grain.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing event content.</param>
internal sealed class InProcessEventSequencesService(
    InProcessGrainFactory grainFactory,
    JsonSerializerOptions jsonSerializerOptions) : Contracts.EventSequences.IEventSequences
{
    /// <inheritdoc/>
    public async Task<AppendResponse> Append(AppendRequest request, ProtoBuf.Grpc.CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.Append(
            request.EventSourceType,
            request.EventSourceId,
            request.EventStreamType,
            request.EventStreamId,
            ToKernelEventType(request.EventType),
            JsonSerializer.Deserialize<JsonNode>(request.Content, jsonSerializerOptions)!.AsObject(),
            request.CorrelationId,
            ToKernelCausation(request.Causation),
            ToKernelIdentity(request.CausedBy),
            request.Tags.Select(t => new KernelConceptsEvents::Tag(t)).ToArray(),
            ToKernelConcurrencyScope(request.ConcurrencyScope),
            request.Occurred);

        return ToContractResponse(result);
    }

    /// <inheritdoc/>
    public async Task<AppendManyResponse> AppendMany(AppendManyRequest request, ProtoBuf.Grpc.CallContext context = default)
    {
        var eventSequence = GetEventSequenceGrain(request);
        var result = await eventSequence.AppendMany(
            request.Events.Select(ToKernelEventToAppend).ToArray(),
            request.CorrelationId,
            ToKernelCausation(request.Causation),
            ToKernelIdentity(request.CausedBy),
            ToKernelConcurrencyScopes(request.ConcurrencyScopes));

        return ToContractResponse(result);
    }

    /// <inheritdoc/>
    public Task<GetTailSequenceNumberResponse> GetTailSequenceNumber(GetTailSequenceNumberRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("GetTailSequenceNumber is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("GetForEventSourceIdAndEventTypes is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<HasEventsForEventSourceIdResponse> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("HasEventsForEventSourceId is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task Revise(ReviseRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("Revise is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(GetFromEventSequenceNumberRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("GetEventsFromEventSequenceNumber is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<RedactResponse> Redact(RedactRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("Redact is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task RedactForEventSource(RedactForEventSourceRequest request, ProtoBuf.Grpc.CallContext context = default) =>
        throw new NotSupportedException("RedactForEventSource is not supported in test scenarios.");

    static KernelConceptsEvents::EventType ToKernelEventType(Contracts.Events.EventType eventType) =>
        new(eventType.Id, eventType.Generation, eventType.Tombstone);

    static KernelConceptsAuditing::Causation[] ToKernelCausation(IEnumerable<Contracts.Auditing.Causation> causations) =>
        causations.Select(c => new KernelConceptsAuditing::Causation(c.Occurred, c.Type, c.Properties ?? new Dictionary<string, string>())).ToArray();

    static KernelConceptsIdentities::Identity ToKernelIdentity(Contracts.Identities.Identity identity) =>
        new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf is not null ? ToKernelIdentity(identity.OnBehalfOf) : null);

    static KernelConceptsConcurrency::ConcurrencyScope ToKernelConcurrencyScope(Contracts.EventSequences.Concurrency.ConcurrencyScope? scope)
    {
        if (scope is null)
        {
            return KernelConceptsConcurrency::ConcurrencyScope.None;
        }

        return new KernelConceptsConcurrency::ConcurrencyScope(
            scope.SequenceNumber,
            scope.EventSourceId,
            string.IsNullOrEmpty(scope.EventStreamType) ? null : new KernelConceptsEvents::EventStreamType(scope.EventStreamType),
            string.IsNullOrEmpty(scope.EventStreamId) ? null : new KernelConceptsEvents::EventStreamId(scope.EventStreamId),
            string.IsNullOrEmpty(scope.EventSourceType) ? null : new KernelConceptsEvents::EventSourceType(scope.EventSourceType),
            scope.EventTypes?.Select(ToKernelEventType).ToArray());
    }

    static KernelConceptsConcurrency::ConcurrencyScopes ToKernelConcurrencyScopes(IDictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope>? scopes)
    {
        if (scopes is null)
        {
            return new KernelConceptsConcurrency::ConcurrencyScopes(new Dictionary<KernelConceptsEvents::EventSourceId, KernelConceptsConcurrency::ConcurrencyScope>());
        }

        return new KernelConceptsConcurrency::ConcurrencyScopes(
            scopes.ToDictionary(
                kvp => (KernelConceptsEvents::EventSourceId)kvp.Key,
                kvp => ToKernelConcurrencyScope(kvp.Value)));
    }

    static AppendResponse ToContractResponse(KernelAppendResult result) =>
        new()
        {
            CorrelationId = result.CorrelationId,
            SequenceNumber = result.SequenceNumber,
            ConstraintViolations = result.ConstraintViolations.Select(ToContractConstraintViolation).ToList(),
            Errors = result.Errors.Select(e => e.Value).ToList(),
            ConcurrencyViolation = result.ConcurrencyViolation is not null ? ToContractConcurrencyViolation(result.ConcurrencyViolation) : null
        };

    static AppendManyResponse ToContractResponse(KernelAppendManyResult result) =>
        new()
        {
            CorrelationId = result.CorrelationId,
            SequenceNumbers = result.SequenceNumbers.Select(s => s.Value).ToList(),
            ConstraintViolations = result.ConstraintViolations.Select(ToContractConstraintViolation).ToList(),
            Errors = result.Errors.Select(e => e.Value).ToList(),
            ConcurrencyViolations = result.ConcurrencyViolations.Select(ToContractConcurrencyViolation).ToList()
        };

    static Contracts.Events.Constraints.ConstraintViolation ToContractConstraintViolation(KernelCore::Cratis.Chronicle.Events.Constraints.ConstraintViolation violation) =>
        new()
        {
            EventTypeId = violation.EventTypeId,
            SequenceNumber = violation.SequenceNumber,
            ConstraintType = (Contracts.Events.Constraints.ConstraintType)violation.ConstraintType,
            ConstraintName = violation.ConstraintName,
            Message = violation.Message,
            Details = violation.Details
        };

    static Contracts.EventSequences.Concurrency.ConcurrencyViolation ToContractConcurrencyViolation(KernelCore::Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyViolation violation) =>
        new()
        {
            ExpectedSequenceNumber = violation.ExpectedSequenceNumber,
            ActualSequenceNumber = violation.ActualSequenceNumber
        };

    KernelEventSequences::IEventSequence GetEventSequenceGrain(IEventSequenceRequest request)
    {
        var key = new KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey(
            (KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId)request.EventSequenceId,
            (KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)request.EventStore,
            (KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName)request.Namespace);

        return grainFactory.GetGrain<KernelEventSequences::IEventSequence>(key, null);
    }

    KernelEventSequences::EventToAppend ToKernelEventToAppend(Contracts.Events.EventToAppend eventToAppend) =>
        new(
            eventToAppend.EventSourceType,
            eventToAppend.EventSourceId,
            eventToAppend.EventStreamType,
            eventToAppend.EventStreamId,
            ToKernelEventType(eventToAppend.EventType),
            eventToAppend.Tags.Select(t => new KernelConceptsEvents::Tag(t)).ToArray(),
            JsonSerializer.Deserialize<JsonNode>(eventToAppend.Content, jsonSerializerOptions)!.AsObject(),
            eventToAppend.Occurred);
}
