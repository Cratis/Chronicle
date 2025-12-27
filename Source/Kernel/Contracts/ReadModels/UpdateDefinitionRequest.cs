// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for updating a read model definition.
/// </summary>
[ProtoContract]
public class UpdateDefinitionRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ReadModelDefinition"/> to update.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public ReadModelDefinition ReadModel { get; set; }
}
