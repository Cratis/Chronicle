// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines the storage for Chronicle users.
/// </summary>
public interface IUserStorage
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<ChronicleUser?> GetById(string id);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<ChronicleUser?> GetByUsername(string username);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<ChronicleUser?> GetByEmail(string email);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Create(ChronicleUser user);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Update(ChronicleUser user);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Delete(string id);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>All users in the system.</returns>
    Task<IEnumerable<ChronicleUser>> GetAll();
}
