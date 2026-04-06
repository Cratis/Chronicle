// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;
using KernelAppendManyResult = KernelCore::Cratis.Chronicle.EventSequences.AppendManyResult;
using KernelConstraintViolation = KernelCore::Cratis.Chronicle.Events.Constraints.ConstraintViolation;
using KernelConcurrencyViolation = KernelCore::Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyViolation;
using KernelAppendError = KernelCore::Cratis.Chronicle.EventSequences.AppendError;
using KernelEventSequences = KernelCore::Cratis.Chronicle.EventSequences;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a thin client adapter over the real kernel <see cref="KernelEventSequences::EventSequence"/> grain
/// that implements the client-facing <see cref="IEventLog"/> and <see cref="IEventSequence"/> interfaces.
/// </summary>
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
            correlationId is null ? null : (KernelConceptsNs::CorrelationId?)ToKernelCorrelationId(correlationId));

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
        if (results.All(r => r.IsSuccess))
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
    public Task<EventSequenceNumber> GetNextSequenceNumber() =>
        Task.FromResult((EventSequenceNumber)grain.State.SequenceNumber.Value);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() =>
        Task.FromResult(grain.State.SequenceNumber.Value == 0
            ? EventSequenceNumber.Unavailable
            : (EventSequenceNumber)(grain.State.SequenceNumber.Value - 1));

    static AppendResult ToClientResult(KernelAppendResult kernelResult) =>
        new()
        {
            CorrelationId = (CorrelationId)(string)kernelResult.CorrelationId,
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
            (ConstraintType)(string)v.ConstraintType,
            (ConstraintName)(string)v.ConstraintName,
            (ConstraintViolationMessage)(string)v.Message,
            (ConstraintViolationDetails)(string)v.Details);

    static ConcurrencyViolation ToClient(KernelConcurrencyViolation v) =>
        new(
            (EventSourceId)(string)v.EventSourceId,
            (EventSequenceNumber)v.ExpectedSequenceNumber.Value,
            (EventSequenceNumber)v.ActualSequenceNumber.Value);

    static KernelConceptsNs::EventSourceId ToKernelEventSourceId(EventSourceId id) =>
        (KernelConceptsNs::EventSourceId)(string)id;

    static KernelConceptsNs::CorrelationId ToKernelCorrelationId(CorrelationId id) =>
        (KernelConceptsNs::CorrelationId)(string)id;
}
