// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request for getting the event sequences a namespace has.
/// </summary>
[ProtoContract]
public class GetEventSequencesRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; } = string.Empty;
}
