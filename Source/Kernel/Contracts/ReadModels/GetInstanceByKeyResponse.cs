// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting a read model instance by key.
/// </summary>
[ProtoContract]
public class GetInstanceByKeyResponse
{
    /// <summary>
    /// Gets or sets the read model as JSON.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of projected events.
    /// </summary>
    [ProtoMember(2)]
    public ulong ProjectedEventsCount { get; set; }

    /// <summary>
    /// Gets or sets the last handled event sequence number.
    /// </summary>
    [ProtoMember(3)]
    public ulong LastHandledEventSequenceNumber { get; set; }
}
