// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Extension methods for <see cref="IChronicleServicesAccessor"/>.
/// </summary>
public static class ChronicleServicesAccessorExtensions
{
    /// <summary>
    /// Gets the available services, waiting for the connection asynchronously and honoring the token when the
    /// accessor supports it (<see cref="IGetServicesAsynchronously"/>); otherwise falls back to
    /// <see cref="IChronicleServicesAccessor.Services"/>.
    /// </summary>
    /// <param name="accessor">The <see cref="IChronicleServicesAccessor"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops waiting for the connection.</param>
    /// <returns>The <see cref="IServices"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the token is cancelled while waiting for the connection.</exception>
    public static ValueTask<IServices> GetServices(this IChronicleServicesAccessor accessor, CancellationToken cancellationToken) =>
        accessor is IGetServicesAsynchronously asynchronous
            ? asynchronous.GetServices(cancellationToken)
            : ValueTask.FromResult(accessor.Services);
}
