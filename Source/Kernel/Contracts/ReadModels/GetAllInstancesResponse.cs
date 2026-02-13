// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting all instances of a read model.
/// </summary>
[ProtoContract]
public class GetAllInstancesResponse
{
    /// <summary>
    /// Gets or sets the collection of instances as JSON strings.
    /// </summary>
    [ProtoMember(1)]
    public IEnumerable<string> Instances { get; set; }

    /// <summary>
    /// Gets or sets the total number of processed events.
    /// </summary>
    [ProtoMember(2)]
    public ulong ProcessedEventsCount { get; set; }
}
