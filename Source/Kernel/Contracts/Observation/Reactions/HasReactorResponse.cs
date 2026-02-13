// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Represents a response indicating whether a reactor exists.
/// </summary>
[ProtoContract]
public class HasReactorResponse
{
    /// <summary>
    /// Gets or sets whether the reactor exists.
    /// </summary>
    [ProtoMember(1)]
    public bool Exists { get; set; }

    /// <summary>
    /// Gets or sets the event sequence id if it exists.
    /// </summary>
    [ProtoMember(2)]
    public string? EventSequenceId { get; set; }
}
