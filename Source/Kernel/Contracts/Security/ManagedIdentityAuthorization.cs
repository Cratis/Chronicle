// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents authorization using a managed or workload identity without a stored secret.
/// </summary>
[ProtoContract]
public class ManagedIdentityAuthorization
{
    /// <summary>
    /// Gets or sets the client identifier, or empty for the system-assigned identity.
    /// </summary>
    [ProtoMember(1)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope requested for the receiver.
    /// </summary>
    [ProtoMember(2)]
    public string Scope { get; set; } = string.Empty;
}
