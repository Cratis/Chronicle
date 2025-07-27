// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the collection of <see cref="ProjectionDefinition"/> instances to register.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<ProjectionDefinition> Projections { get; set; } = [];
}
