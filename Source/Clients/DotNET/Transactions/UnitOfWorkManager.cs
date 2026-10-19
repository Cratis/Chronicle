// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWorkManager"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
/// <param name="lifecyclePolicy">The policy for staging after completion begins.</param>
/// <param name="logger">Optional logger for late staging diagnostics.</param>
public class UnitOfWorkManager(
    IEventStore eventStore,
    IActivitySource<UnitOfWork>? activitySource,
    UnitOfWorkLifecyclePolicy lifecyclePolicy,
    ILogger<UnitOfWork>? logger = null) : IUnitOfWorkManager
{
    static readonly AsyncLocal<CurrentFrame?> _current = new();
    readonly ConcurrentDictionary<CorrelationId, IUnitOfWork> _unitsOfWork = new();

    /// <summary>
    /// Initializes a manager with compatibility staging behavior.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="activitySource">Optional activity source.</param>
    public UnitOfWorkManager(IEventStore eventStore, IActivitySource<UnitOfWork>? activitySource = null)
        : this(eventStore, activitySource, UnitOfWorkLifecyclePolicy.Compatibility)
    {
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
            eventStore,
            activitySource,
            lifecyclePolicy,
            logger);
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
