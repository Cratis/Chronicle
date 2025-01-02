// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the payload for registering an observer.
/// </summary>
[ProtoContract]
public class RegisterReducer
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(2)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(3)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ReducerDefinition"/>.
    /// </summary>
    [ProtoMember(4)]
    public ReducerDefinition Reducer { get; set; }
}
