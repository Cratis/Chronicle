// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting snapshots by read model key.
/// </summary>
[ProtoContract]
public class GetSnapshotsByKeyResponse
{
    /// <summary>
    /// Gets or sets the collection of snapshots.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<ReadModelSnapshot> Snapshots { get; set; } = [];
}
