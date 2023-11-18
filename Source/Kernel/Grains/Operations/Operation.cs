// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Represents an implementation of <see cref="IOperation{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the operation.</typeparam>
public class Operation<TRequest> : Grain<OperationState>, IOperation<TRequest>
{
    /// <inheritdoc/>
    public virtual Task Perform() => Task.CompletedTask;
}
