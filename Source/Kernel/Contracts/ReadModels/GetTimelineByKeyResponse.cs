// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting a read model's timeline by key.
/// </summary>
[ProtoContract]
public class GetTimelineByKeyResponse
{
    /// <summary>
    /// Gets or sets the timeline, one entry per event, oldest first.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<ReadModelTimelineEntry> Entries { get; set; } = [];
}
