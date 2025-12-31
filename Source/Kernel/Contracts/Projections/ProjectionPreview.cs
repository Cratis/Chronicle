// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a preview of a projection.
/// </summary>
[ProtoContract]
public class ProjectionPreview
{
    /// <summary>
    /// The read model entries resulting from the projection preview.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IEnumerable<string> ReadModelEntries { get; set; } = []
}
