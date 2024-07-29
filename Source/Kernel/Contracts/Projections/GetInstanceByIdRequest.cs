// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for getting an instance by id.
/// </summary>
[ProtoContract]
public class GetInstanceByIdRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the projection id.
    /// </summary>
    [ProtoMember(4)]
    public string ProjectionId { get; set; }

    /// <summary>
    /// Gets or sets the model key.
    /// </summary>
    [ProtoMember(5)]
    public string ModelKey { get; set; }
}
