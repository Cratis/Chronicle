// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for registering many read models.
/// </summary>
[ProtoContract]
public class RegisterManyRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the owner of the read model.
    /// </summary>
    [ProtoMember(2)]
    public ReadModelOwner Owner { get; set; } = ReadModelOwner.None;

    /// <summary>
    /// Gets or sets the collection of <see cref="ReadModelDefinition"/> instances to register.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<ReadModelDefinition> ReadModels { get; set; } = [];

    /// <summary>
    /// Gets or sets the source of the read model.
    /// </summary>
    [ProtoMember(4)]
    [DefaultValue(ReadModelSource.Code)]
    public ReadModelSource Source { get; set; } = ReadModelSource.Code;
}
