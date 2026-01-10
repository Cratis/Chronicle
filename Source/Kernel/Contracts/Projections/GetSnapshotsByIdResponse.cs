// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the response for getting snapshots by CorrelationId.
/// </summary>
[ProtoContract]
public class GetSnapshotsByIdResponse
{
    /// <summary>
    /// Gets or sets the collection of snapshots.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<ProjectionSnapshot> Snapshots { get; set; } = [];
}
