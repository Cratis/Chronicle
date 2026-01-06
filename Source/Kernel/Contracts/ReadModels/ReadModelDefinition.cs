// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sinks;

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
    /// Gets or sets the projection sink definition.
    /// </summary>
    [ProtoMember(3)]
    public SinkDefinition Sink { get; set; }

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

    /// <summary>
    /// Gets or sets the type of owner for the read model.
    /// </summary>
    [ProtoMember(7)]
    public ReadModelObserverType ObserverType { get; set; }

    /// <summary>
    /// Gets or sets the observer identifier for the read model.
    /// </summary>
    [ProtoMember(8)]
    public string ObserverIdentifier { get; set; } = string.Empty;
}
