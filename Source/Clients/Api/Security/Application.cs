// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents application in the Chronicle system.
/// </summary>
/// <param name="Id">The unique identifier of the application.</param>
/// <param name="ClientId">The unique identifier of the client associated with the application.</param>
/// <param name="IsActive">Indicates whether the application is active.</param>
/// <param name="CreatedAt">The date and time when the application was created.</param>
/// <param name="LastModifiedAt">The date and time when the application was last modified.</param>
public record Application(
    string Id,
    string ClientId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);
