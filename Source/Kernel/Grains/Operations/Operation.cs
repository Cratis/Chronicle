// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Represents an implementation of <see cref="IOperation{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the operation.</typeparam>
public class Operation<TRequest> : Grain<OperationState>, IOperation<TRequest>
    where TRequest : class
{
    /// <inheritdoc/>
    public async Task Perform()
    {
        var request = (State.Request as TRequest)!;
        await OnPerform(request);
        await ClearStateAsync();
    }

    /// <summary>
    /// THe method that gets called when the operation is performed.
    /// </summary>
    /// <param name="request">The request for the operation.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnPerform(TRequest request) => Task.CompletedTask;
}
