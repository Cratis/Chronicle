// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents the payload for getting the saved event sequence queries an owner can see.
/// </summary>
[ProtoContract]
public class GetSequenceQueriesRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity to get queries for - their own, plus everything shared with everyone.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Owner { get; set; } = string.Empty;
}
