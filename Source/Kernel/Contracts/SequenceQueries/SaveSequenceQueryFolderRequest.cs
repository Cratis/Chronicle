// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents the payload for saving a folder in the saved query hierarchy.
/// </summary>
[ProtoContract]
public class SaveSequenceQueryFolderRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder to save.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public SequenceQueryFolderDefinition Folder { get; set; } = new();
}
