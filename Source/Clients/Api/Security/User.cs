// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents a user in the Chronicle system.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="IsActive">Indicates whether the user is active.</param>
/// <param name="CreatedAt">The date and time when the user was created.</param>
/// <param name="LastModifiedAt">The date and time when the user was last modified.</param>
public record User(string Id, string Username, string? Email, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? LastModifiedAt);
