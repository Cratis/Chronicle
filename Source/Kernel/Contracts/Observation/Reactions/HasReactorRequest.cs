// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Represents a request to check if a reactor exists.
/// </summary>
[ProtoContract]
public class HasReactorRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the reactor id.
    /// </summary>
    [ProtoMember(3)]
    public string ReactorId { get; set; }
}
