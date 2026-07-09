// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request to get all event sequences for an event store and namespace.
/// </summary>
[ProtoContract]
public class GetAllEventSequencesRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; }
}
