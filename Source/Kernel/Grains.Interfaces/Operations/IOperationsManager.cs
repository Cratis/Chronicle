// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Operations;

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Defines a system that manages operations.
/// </summary>
public interface IOperationsManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Add an operation.
    /// </summary>
    /// <param name="request">The request for the operation.</param>
    /// <typeparam name="TOperation">Type of operation to add.</typeparam>
    /// <typeparam name="TRequest">Type of request for the operation.</typeparam>
    /// <returns>The <see cref="OperationId"/> for the added operation.</returns>
    Task<OperationId> Add<TOperation, TRequest>(TRequest request)
        where TOperation : IOperation<TRequest>
        where TRequest : class;
}
