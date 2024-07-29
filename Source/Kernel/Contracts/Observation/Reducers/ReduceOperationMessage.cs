// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the message from the server for handling a reduce operation.
/// </summary>
[ProtoContract]
public class ReduceOperationMessage
{
    /// <summary>
    /// Gets or sets the initial state.
    /// </summary>
    [ProtoMember(1)]
    public string? InitialState { get; set; }

    /// <summary>
    /// Gets or sets a collection of <see cref="AppendedEvent"/>.
    /// </summary>
    [ProtoMember(2)]
    public IList<AppendedEvent> Events { get; set; }
}
