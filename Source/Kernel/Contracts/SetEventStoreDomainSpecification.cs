// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Represents the command for setting the domain specification of an event store.
/// </summary>
[ProtoContract]
public record SetEventStoreDomainSpecification
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the domain specification.
    /// </summary>
    [ProtoMember(2)]
    public string Specification { get; set; }
}
