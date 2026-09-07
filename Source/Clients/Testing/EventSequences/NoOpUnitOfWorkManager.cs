// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op <see cref="IUnitOfWorkManager"/> for the in-process kernel command pipeline.
/// </summary>
/// <remarks>
/// <c>AddCratisArcCore</c> discovers every <c>ICommandExecutionScope</c> across the
/// whole process, not just the ones a given service collection cares about - an Arc.Chronicle consumer's
/// transactional command scope is discovered here too, even though this pipeline only ever executes the kernel's
/// own commands, which append directly through the grain and never touch a unit of work. Without a registration,
/// that scope falls back to auto-activating the real client <c>EventStore</c>, which needs a live connection this
/// in-process kernel does not have. This satisfies the resolution harmlessly instead.
/// </remarks>
internal sealed class NoOpUnitOfWorkManager : IUnitOfWorkManager
{
    /// <inheritdoc/>
    public IUnitOfWork Current => throw new NoUnitOfWorkHasBeenStarted();

    /// <inheritdoc/>
    public bool HasCurrent => false;

    /// <inheritdoc/>
    public bool TryGetFor(CorrelationId correlationId, [MaybeNullWhen(false)] out IUnitOfWork unitOfWork)
    {
        unitOfWork = default;
        return false;
    }

    /// <inheritdoc/>
    public IUnitOfWork Begin(CorrelationId correlationId) => new NoOpUnitOfWork(correlationId);

    /// <inheritdoc/>
    public void SetCurrent(IUnitOfWork unitOfWork)
    {
    }

    sealed class NoOpUnitOfWork(CorrelationId correlationId) : IUnitOfWork
    {
        public bool IsCompleted => true;
        public CorrelationId CorrelationId => correlationId;
        public bool IsSuccess => true;

        public void AddEvent(
            EventSequenceId eventSequenceId,
            EventSourceId eventSourceId,
            object @event,
            Causation causation,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            ConcurrencyScope? concurrencyScope = default,
            IEnumerable<string>? tags = default,
            DateTimeOffset? occurred = default,
            Subject? subject = default)
        {
        }

        public IEnumerable<object> GetEvents() => [];
        public IEnumerable<ConstraintViolation> GetConstraintViolations() => [];
        public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => [];
        public IEnumerable<AppendError> GetAppendErrors() => [];
        public Task Commit() => Task.CompletedTask;
        public Task Rollback() => Task.CompletedTask;
        public void OnCompleted(Action<IUnitOfWork> callback)
        {
        }

        public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
        {
            eventSequenceNumber = default;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
