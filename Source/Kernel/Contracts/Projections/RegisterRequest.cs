// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for registering projections.
/// </summary>
[ProtoContract]
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ProjectionDefinition"/> instances to register.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IEnumerable<ProjectionDefinition> Projections { get; set; } = [];
}
