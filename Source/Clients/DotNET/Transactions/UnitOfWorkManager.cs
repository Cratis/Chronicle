// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWorkManager"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
public class UnitOfWorkManager(IEventStore eventStore) : IUnitOfWorkManager
{
    static readonly AsyncLocal<IUnitOfWork> _current = new();

    /// <inheritdoc/>
    public IUnitOfWork Current => _current.Value ?? throw new NoUnitOfWorkHasBeenStarted();

    /// <inheritdoc/>
    public bool HasCurrent => _current.Value != null;

    /// <inheritdoc/>
    public IUnitOfWork Begin(CorrelationId correlationId)
    {
        var unitOfWork = new UnitOfWork(correlationId, eventStore);
        _current.Value = unitOfWork;
        return unitOfWork;
    }
}
