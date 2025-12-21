// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents client credentials in the Chronicle system.
/// </summary>
[ProtoContract]
public record ClientCredentials
{
    /// <summary>
    /// The unique identifier for the client.
    /// </summary>
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The client identifier.
    /// </summary>
    [ProtoMember(2)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the client is active.
    /// </summary>
    [ProtoMember(3)]
    public bool IsActive { get; set; }

    /// <summary>
    /// When the client was created.
    /// </summary>
    [ProtoMember(4)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the client was last modified.
    /// </summary>
    [ProtoMember(5)]
    public DateTimeOffset? LastModifiedAt { get; set; }
}
