// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Identities;

/// <summary>
/// Represents the request for renaming the display name of an identity.
/// </summary>
[ProtoContract]
public class RenameIdentityRequest
{
    /// <summary>
    /// Gets or sets the event store to rename the identity in.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace to rename the identity in.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject that identifies the identity to rename.
    /// </summary>
    [ProtoMember(3)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new name to set on the identity.
    /// </summary>
    [ProtoMember(4)]
    public string Name { get; set; } = string.Empty;
}
