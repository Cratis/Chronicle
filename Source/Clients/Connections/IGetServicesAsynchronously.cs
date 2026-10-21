// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Optional capability of an <see cref="IChronicleServicesAccessor"/> that can obtain the services without blocking
/// the calling thread and while honoring a <see cref="CancellationToken"/>.
/// </summary>
public interface IGetServicesAsynchronously
{
    /// <summary>
    /// Gets the available services, connecting first when the connection is not established.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops waiting for the connection.</param>
    /// <returns>The <see cref="IServices"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the token is cancelled while waiting. The connect attempt itself keeps running for other callers.</exception>
    ValueTask<IServices> GetServices(CancellationToken cancellationToken);
}
