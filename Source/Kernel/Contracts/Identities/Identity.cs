// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Aksio.Cratis.Kernel.Contracts.Identities;

/// <summary>
/// Represents the payload for an identity.
/// </summary>
[DataContract]
public class Identity
{
    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    [DataMember(Order = 1)]
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [DataMember(Order = 2)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [DataMember(Order = 3)]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the on behalf of.
    /// </summary>
    [DataMember(Order = 4)]
    public Identity? OnBehalfOf { get; set; }
}
