// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Defines the contract for working with users.
/// </summary>
[Service]
public interface IUsers
{
    /// <summary>
    /// Add a new user.
    /// </summary>
    /// <param name="command">The <see cref="AddUser"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddUser command);

    /// <summary>
    /// Remove a user.
    /// </summary>
    /// <param name="command">The <see cref="RemoveUser"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveUser command);

    /// <summary>
    /// Change a user's password.
    /// </summary>
    /// <param name="command">The <see cref="ChangeUserPassword"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ChangePassword(ChangeUserPassword command);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>Collection of <see cref="User"/>.</returns>
    [Operation]
    Task<IEnumerable<User>> GetAll();

    /// <summary>
    /// Observe all users.
    /// </summary>
    /// <param name="context">The gRPC <see cref="CallContext"/>.</param>
    /// <returns>An observable of collection of <see cref="User"/>.</returns>
    [Operation]
    IObservable<IEnumerable<User>> ObserveAll(CallContext context = default);
}
