// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for changing client credentials secret.
/// </summary>
[ProtoContract]
public record ChangeClientCredentialsSecret
{
    /// <summary>
    /// The unique identifier for the client.
    /// </summary>
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The new client secret.
    /// </summary>
    [ProtoMember(2)]
    public string ClientSecret { get; set; } = string.Empty;
}
