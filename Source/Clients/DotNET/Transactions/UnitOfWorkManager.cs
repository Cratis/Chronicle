// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWorkManager"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
public class UnitOfWorkManager(IEventStore eventStore) : IUnitOfWorkManager
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
            eventStore);
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
        _current.Value = null!;
    }
}
