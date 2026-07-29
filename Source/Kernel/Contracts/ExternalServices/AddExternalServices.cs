// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the payload for adding external services.
/// </summary>
[ProtoContract]
public class AddExternalServices
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the collection of <see cref="ExternalServiceDefinition"/> instances to add.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<ExternalServiceDefinition> ExternalServices { get; set; } = [];
}
