// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWorkManager"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
public class UnitOfWorkManager(
    IEventStore eventStore,
    IActivitySource<UnitOfWork>? activitySource = null) : IUnitOfWorkManager
{
    static readonly AsyncLocal<IUnitOfWork> _current = new();
    readonly ConcurrentDictionary<CorrelationId, IUnitOfWork> _unitsOfWork = new();

    /// <inheritdoc/>
    public IUnitOfWork Current => _current.Value ?? throw new NoUnitOfWorkHasBeenStarted();

    /// <inheritdoc/>
    public bool HasCurrent => _current.Value is not null;

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
            activitySource);
        _current.Value = unitOfWork;
        _unitsOfWork[correlationId] = unitOfWork;
        return unitOfWork;
    }

    /// <inheritdoc/>
    public void SetCurrent(IUnitOfWork unitOfWork)
    {
        _current.Value = unitOfWork;
        _unitsOfWork[unitOfWork.CorrelationId] = unitOfWork;
        unitOfWork.OnCompleted(UnitOfWorkCompleted);
    }

    void UnitOfWorkCompleted(IUnitOfWork unitOfWork)
    {
        _unitsOfWork.TryRemove(unitOfWork.CorrelationId, out _);

        // Only clear Current when the completing unit is the current one - clearing unconditionally
        // would wipe a different unit that became current while this one was still live.
        if (ReferenceEquals(_current.Value, unitOfWork))
        {
            _current.Value = null!;
        }
    }
}
