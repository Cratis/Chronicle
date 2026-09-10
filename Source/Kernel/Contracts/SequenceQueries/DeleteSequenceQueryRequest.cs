// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents the payload for deleting a saved event sequence query.
/// </summary>
[ProtoContract]
public class DeleteSequenceQueryRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the query to delete.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Id { get; set; } = string.Empty;
}
