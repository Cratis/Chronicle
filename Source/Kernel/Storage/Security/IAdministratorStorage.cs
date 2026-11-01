// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines storage for the administrator privilege projection, separate from users.
/// </summary>
public interface IAdministratorStorage
{
    /// <summary>
    /// Gets all granted administrator privileges.
    /// </summary>
    /// <returns>The privileges, including those of inactive users.</returns>
    Task<IEnumerable<Administrator>> GetAll();

    /// <summary>
    /// Stores a grant, replacing any existing grant for the user.
    /// </summary>
    /// <param name="administrator">The privilege to store.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(Administrator administrator);

    /// <summary>
    /// Removes a privilege, if present.
    /// </summary>
    /// <param name="userId">The user losing the privilege.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(UserId userId);
}
