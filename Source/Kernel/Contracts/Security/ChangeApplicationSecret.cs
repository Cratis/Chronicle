// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for changing application secret.
/// </summary>
[ProtoContract]
public class ChangeApplicationSecret
{
    /// <summary>
    /// The unique identifier for the client.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The new client secret.
    /// </summary>
    [ProtoMember(2)]
    public string ClientSecret { get; set; } = string.Empty;
}
