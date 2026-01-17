// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Defines the contract for working with applications.
/// </summary>
[Service]
public interface IApplications
{
    /// <summary>
    /// Add new applications.
    /// </summary>
    /// <param name="command">The <see cref="AddApplication"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddApplication command);

    /// <summary>
    /// Remove applications.
    /// </summary>
    /// <param name="command">The <see cref="RemoveApplication"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveApplication command);

    /// <summary>
    /// Change applications secret.
    /// </summary>
    /// <param name="command">The <see cref="ChangeApplicationSecret"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ChangeSecret(ChangeApplicationSecret command);

    /// <summary>
    /// Gets all applications.
    /// </summary>
    /// <returns><see cref="IList{T}"/> of <see cref="Application"/>.</returns>
    [Operation]
    Task<IList<Application>> GetAll();

    /// <summary>
    /// Observe all applications.
    /// </summary>
    /// <param name="context">The gRPC <see cref="CallContext"/>.</param>
    /// <returns>An observable of <see cref="IList{T}"/> of <see cref="Application"/>.</returns>
    [Operation]
    IObservable<IList<Application>> ObserveAll(CallContext context = default);
}
