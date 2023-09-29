// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Identities;

/// <summary>
/// Represents the payload for an identity.
/// </summary>
[ProtoContract]
public class Identity
{
    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    [ProtoMember(1)]
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [ProtoMember(3)]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the on behalf of.
    /// </summary>
    [ProtoMember(4)]
    public Identity? OnBehalfOf { get; set; }
}
