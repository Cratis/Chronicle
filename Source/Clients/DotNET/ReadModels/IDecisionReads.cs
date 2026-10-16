// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels;

/// <summary>Reads models with guards for a subsequent append.</summary>
public interface IDecisionReads
{
    /// <summary>Reads and enrolls into the ambient unit of work.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The raw source key.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Reads without enrolling; pass the result to a unit of work or guarded append.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The raw source key.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Reads one event stream and enrolls the read into the ambient unit of work.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="eventSourceType">The optional event source type filter.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    /// <exception cref="DecisionReadRefused">The reader does not support stream-scoped reads.</exception>
    Task<DecisionRead<T>> Get<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, typeof(T));

    /// <summary>
    /// Reads one event stream without enrolling; pass the result to a unit of work or guarded append.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="eventSourceType">The optional event source type filter.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The guarded read.</returns>
    /// <exception cref="DecisionReadRefused">The reader does not support stream-scoped reads.</exception>
    Task<DecisionRead<T>> GetDetached<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, typeof(T));

    /// <summary>
    /// Checks whether the projection shape can be read within one event stream without I/O.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <returns>The admission result.</returns>
    /// <exception cref="DecisionReadRefused">The reader does not support stream-scoped reads.</exception>
    DecisionReadAdmission AdmitForStream<T>()
        where T : class => throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, typeof(T));

    /// <summary>
    /// Acquires and enrolls a fresh read into the given unit before returning.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="unitOfWork">The receiving unit, which must own the active frame.</param>
    /// <param name="key">The raw source key.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The enrolled read.</returns>
    /// <exception cref="UnitOfWorkReadProtectionNotSupported">The reader does not support explicit unit acquisition.</exception>
    Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class => throw new UnitOfWorkReadProtectionNotSupported(GetType());

    /// <summary>
    /// Acquires and enrolls one event stream into the given unit before returning.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="unitOfWork">The receiving unit, which must own the active frame.</param>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <param name="eventSourceType">The optional event source type.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The enrolled read.</returns>
    /// <exception cref="UnitOfWorkReadProtectionNotSupported">The reader does not support explicit unit acquisition.</exception>
    Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => throw new UnitOfWorkReadProtectionNotSupported(GetType());

    /// <summary>
    /// Describes local shape admission, guarded types and generation delivery without I/O.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <returns>The local description.</returns>
    /// <exception cref="DecisionReadRefused">The reader does not support local descriptions.</exception>
    DecisionReadDescription Describe<T>()
        where T : class => throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, typeof(T));

    /// <summary>Checks the projection shape without I/O.</summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <returns>The admission result.</returns>
    DecisionReadAdmission Admit<T>()
        where T : class;
}
