// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents a client credential for OAuth client_credentials flow.
/// </summary>
/// <param name="Id">The unique identifier for the client.</param>
/// <param name="ClientId">The client identifier.</param>
/// <param name="ClientSecret">The hashed client secret.</param>
/// <param name="IsActive">Whether the client is active.</param>
/// <param name="CreatedAt">When the client was created.</param>
/// <param name="LastModifiedAt">When the client was last modified.</param>
public record ChronicleClient(
    string Id,
    string ClientId,
    string ClientSecret,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);
