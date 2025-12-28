// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the definition of a read model.
/// </summary>
[ProtoContract]
public class ReadModelDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the model.
    /// </summary>
    [ProtoMember(1)]
    public ReadModelType Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the sink type the read model is associated with.
    /// </summary>
    [ProtoMember(3)]
    public Guid SinkType { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the sink configuration the read model is associated with.
    /// </summary>
    [ProtoMember(4)]
    public Guid SinkConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for the model.
    /// </summary>
    [ProtoMember(5)]
    public string Schema { get; set; }

    /// <summary>
    /// Gets or sets the indexes defined for the model.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IList<IndexDefinition> Indexes { get; set; } = [];
}
