// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.SyncWork.Enums;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IStep{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object.</typeparam>
/// <typeparam name="TResponse">Type of response object.</typeparam>
public class Step<TRequest, TResponse> : IStep<TRequest, TResponse>
{
    /// <inheritdoc/>
    public Task<Exception> GetException() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<TResponse> GetResult() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<SyncWorkStatus> GetWorkStatus() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<bool> Start(TRequest request) => throw new NotImplementedException();
}
