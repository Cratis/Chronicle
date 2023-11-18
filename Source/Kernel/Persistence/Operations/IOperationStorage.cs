// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Operations;
using Aksio.Cratis.Kernel.Operations;

namespace Aksio.Cratis.Kernel.Persistence.Operations;

/// <summary>
/// Defines the storage for <see cref="OperationState"/>.
/// </summary>
public interface IOperationStorage
{
    /// <summary>
    /// Get an operation by its <see cref="OperationId"/>.
    /// </summary>
    /// <param name="operationId">The <see cref="OperationId"/> to get for.</param>
    /// <returns>An <see cref="OperationState"/> if it was found, null if not.</returns>
    Task<OperationState?> Get(OperationId operationId);

    /// <summary>
    /// Save an operation.
    /// </summary>
    /// <param name="operationId">The <see cref="OperationId"/> to save for.</param>
    /// <param name="operationState">The <see cref="OperationState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(OperationId operationId, OperationState operationState);

    /// <summary>
    /// Remove a job.
    /// </summary>
    /// <param name="operationId">The <see cref="OperationId"/> of the operation to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(OperationId operationId);

    /// <summary>
    /// Get all operations.
    /// </summary>
    /// <returns>A collection of <see cref="OperationState"/>.</returns>
    Task<IImmutableList<OperationState>> GetOperations();

    /// <summary>
    /// Observe all operations.
    /// </summary>
    /// <returns>An observable of collection of operations.</returns>
    IObservable<IEnumerable<OperationState>> ObserveOperations();
}
