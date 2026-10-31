// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWorkManager"/>.
/// </summary>
public class UnitOfWorkManager : IUnitOfWorkManager
{
    static readonly AsyncLocal<CurrentFrame?> _current = new();
    readonly ConcurrentDictionary<CorrelationId, IUnitOfWork> _unitsOfWork = new();
    readonly Func<IEventStore> _eventStore;
    readonly IActivitySource<UnitOfWork>? _activitySource;
    readonly UnitOfWorkLifecyclePolicy _lifecyclePolicy;
    readonly ILogger<UnitOfWork>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkManager"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="activitySource">Optional activity source.</param>
    /// <param name="lifecyclePolicy">The policy for staging after completion begins.</param>
    /// <param name="logger">Optional logger for late staging diagnostics.</param>
    public UnitOfWorkManager(
        IEventStore eventStore,
        IActivitySource<UnitOfWork>? activitySource,
        UnitOfWorkLifecyclePolicy lifecyclePolicy,
        ILogger<UnitOfWork>? logger = null)
        : this(() => eventStore, activitySource, lifecyclePolicy, logger)
    {
    }

    /// <summary>
    /// Initializes a manager with compatibility staging behavior.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="activitySource">Optional activity source.</param>
    public UnitOfWorkManager(IEventStore eventStore, IActivitySource<UnitOfWork>? activitySource = null)
        : this(eventStore, activitySource, UnitOfWorkLifecyclePolicy.Compatibility)
    {
    }

    /// <summary>
    /// Initializes a manager that binds each unit's event store on first use, not at request startup.
    /// </summary>
    /// <param name="eventStore">Resolves the event store in the owning scope.</param>
    /// <param name="activitySource">Optional activity source.</param>
    /// <param name="lifecyclePolicy">The policy for staging after completion begins.</param>
    /// <param name="logger">Optional logger for late staging diagnostics.</param>
    internal UnitOfWorkManager(
        Func<IEventStore> eventStore,
        IActivitySource<UnitOfWork>? activitySource,
        UnitOfWorkLifecyclePolicy lifecyclePolicy,
        ILogger<UnitOfWork>? logger)
    {
        _eventStore = eventStore;
        _activitySource = activitySource;
        _lifecyclePolicy = lifecyclePolicy;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IUnitOfWork Current => OpenFrame()?.UnitOfWork ?? throw new NoUnitOfWorkHasBeenStarted();

    /// <inheritdoc/>
    public bool HasCurrent => OpenFrame() is not null;

    /// <inheritdoc/>
    public bool TryGetFor(CorrelationId correlationId, [MaybeNullWhen(false)] out IUnitOfWork unitOfWork) =>
        _unitsOfWork.TryGetValue(correlationId, out unitOfWork);

    /// <inheritdoc/>
    public IUnitOfWork Begin(CorrelationId correlationId)
    {
        var unitOfWork = new UnitOfWork(
            correlationId,
            UnitOfWorkCompleted,
            _eventStore,
            _activitySource,
            _lifecyclePolicy,
            _logger);
        ImmediateAppendGuard.RequireUnit(unitOfWork);
        ReadProtectionGuard.RequireUnit(unitOfWork);
        _current.Value = new CurrentFrame(unitOfWork, OpenFrame());
        _unitsOfWork[correlationId] = unitOfWork;
        return unitOfWork;
    }

    /// <inheritdoc/>
    public void SetCurrent(IUnitOfWork unitOfWork)
    {
        ImmediateAppendGuard.RequireUnit(unitOfWork);
        ReadProtectionGuard.RequireUnit(unitOfWork);
        _current.Value = new CurrentFrame(unitOfWork, OpenFrame());
        _unitsOfWork[unitOfWork.CorrelationId] = unitOfWork;
        unitOfWork.OnCompleted(UnitOfWorkCompleted);
    }

    /// <summary>
    /// Gets the nearest still-open unit: a completed unit is skipped so the unit that was current when it began
    /// becomes current again, in the flow that began it, without relying on the completion callback's flow.
    /// </summary>
    /// <returns>The nearest open frame, if any.</returns>
    static CurrentFrame? OpenFrame()
    {
        var frame = _current.Value;
        while (frame?.UnitOfWork.IsCompleted == true)
        {
            frame = frame.Previous;
        }
        return frame;
    }

    void UnitOfWorkCompleted(IUnitOfWork unitOfWork) => _unitsOfWork.TryRemove(unitOfWork.CorrelationId, out _);

    sealed record CurrentFrame(IUnitOfWork UnitOfWork, CurrentFrame? Previous);
}
