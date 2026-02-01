// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents a user in the Chronicle system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public UserId Id { get; set; } = UserId.NotSet;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public Username Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public UserEmail? Email { get; set; }

    /// <summary>
    /// Gets or sets the hashed password.
    /// </summary>
    public UserPassword? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets a random value that should change whenever user credentials change.
    /// </summary>
    public SecurityStamp? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets when the user was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the user must change their password on next login.
    /// </summary>
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// Gets or sets whether the user has ever logged in and set their password.
    /// This is primarily used for the initial admin user who starts without a password.
    /// </summary>
    public bool HasLoggedIn { get; set; }
}
