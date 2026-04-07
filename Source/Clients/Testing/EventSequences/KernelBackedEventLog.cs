// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;
using KernelConcurrencyViolation = KernelCore::Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyViolation;
using KernelConstraintViolation = KernelCore::Cratis.Chronicle.Events.Constraints.ConstraintViolation;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a thin client adapter over the real kernel <see cref="KernelEventSequences::EventSequence"/> grain
/// that implements the client-facing <see cref="IEventLog"/> and <see cref="IEventSequence"/> interfaces.
/// </summary>
/// <param name="grain">The kernel <see cref="KernelEventSequences::EventSequence"/> grain to delegate to.</param>
/// <remarks>
/// All business logic (constraint checking, hash calculation, serialization, migration) runs through
/// the real kernel grain. This adapter only translates parameter types and result types between
/// the client and kernel APIs.
/// </remarks>
internal sealed class KernelBackedEventLog(KernelEventSequences::EventSequence grain) : IEventLog
{
    /// <inheritdoc/>
    public EventSequenceId Id => EventSequenceId.Log;

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => throw new NotSupportedException("Transactional sequences are not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> filterEventTypes,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default) =>
        throw new NotSupportedException("GetForEventSourceIdAndEventTypes is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) =>
        throw new NotSupportedException("HasEventsFor is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? filterEventTypes = default) =>
        throw new NotSupportedException("GetFromSequenceNumber is not supported in test scenarios.");

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetNextSequenceNumber()
    {
        var result = await grain.GetNextSequenceNumber();
        return (EventSequenceNumber)result.Value;
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default) =>
        grain.GetTailSequenceNumber().ContinueWith(t => (EventSequenceNumber)t.Result.Value, TaskScheduler.Default);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) =>
        throw new NotSupportedException("GetTailSequenceNumberForObserver is not supported in test scenarios.");

    /// <inheritdoc/>
    public async Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default)
    {
        var kernelResult = await grain.Append(
            ToKernelEventSourceId(eventSourceId),
            @event,
            correlationId);

        return ToClientResult(kernelResult);
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default)
    {
        var results = new List<AppendResult>();
        foreach (var @event in events)
        {
            var result = await Append(eventSourceId, @event, eventStreamType, eventStreamId, eventSourceType, correlationId, tags, concurrencyScope, occurred);
            results.Add(result);
            if (!result.IsSuccess)
            {
                break;
            }
        }

        var correlationIdValue = results.FirstOrDefault()?.CorrelationId ?? CorrelationId.New();
        if (results.TrueForAll(r => r.IsSuccess))
        {
            return new AppendManyResult
            {
                CorrelationId = correlationIdValue,
                SequenceNumbers = results.Select(r => r.SequenceNumber).ToImmutableList(),
                ConstraintViolations = [],
                Errors = [],
                ConcurrencyViolations = []
            };
        }

        var failed = results.First(r => !r.IsSuccess);
        return new AppendManyResult
        {
            CorrelationId = correlationIdValue,
            SequenceNumbers = results.Where(r => r.IsSuccess).Select(r => r.SequenceNumber).ToImmutableList(),
            ConstraintViolations = failed.ConstraintViolations.ToImmutableList(),
            Errors = failed.Errors.ToImmutableList(),
            ConcurrencyViolations = failed.ConcurrencyViolation is not null
                ? [failed.ConcurrencyViolation]
                : []
        };
    }

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default) =>
        throw new NotSupportedException("AppendMany with EventForEventSourceId is not supported in test scenarios; use the EventSourceId overload instead.");

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason) =>
        throw new NotSupportedException("Redact is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes) =>
        throw new NotSupportedException("Redact is not supported in test scenarios.");

    static AppendResult ToClientResult(KernelAppendResult kernelResult) =>
        new()
        {
            CorrelationId = kernelResult.CorrelationId,
            SequenceNumber = (EventSequenceNumber)kernelResult.SequenceNumber.Value,
            ConstraintViolations = kernelResult.ConstraintViolations.Select(ToClient).ToImmutableList(),
            Errors = kernelResult.Errors.Select(e => (AppendError)(string)e).ToImmutableList(),
            ConcurrencyViolation = kernelResult.ConcurrencyViolation is not null
                ? ToClient(kernelResult.ConcurrencyViolation)
                : null
        };

    static ConstraintViolation ToClient(KernelConstraintViolation v) =>
        new(
            (EventTypeId)(string)v.EventTypeId,
            (EventSequenceNumber)v.SequenceNumber.Value,
            (ConstraintType)(int)v.ConstraintType,
            (ConstraintName)(string)v.ConstraintName,
            (ConstraintViolationMessage)(string)v.Message,
            new ConstraintViolationDetails(v.Details));

    static ConcurrencyViolation ToClient(KernelConcurrencyViolation v) =>
        new(
            (EventSourceId)(string)v.EventSourceId,
            (EventSequenceNumber)v.ExpectedSequenceNumber.Value,
            (EventSequenceNumber)v.ActualSequenceNumber.Value);

    static KernelEvents.EventSourceId ToKernelEventSourceId(EventSourceId id) =>
        (KernelEvents.EventSourceId)(string)id;
}
