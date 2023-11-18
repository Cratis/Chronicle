// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Operations;

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Represents an implementation of <see cref="IOperationsManager"/> that has a result.
/// </summary>
public class OperationsManager : Grain, IOperationsManager
{
    /// <inheritdoc/>
    public Task<OperationId> Add<TOperation, TRequest>(TRequest request)
        where TOperation : IOperation<TRequest>
        where TRequest : class => throw new NotImplementedException();
}
