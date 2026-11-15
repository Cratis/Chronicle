// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Sinks;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
[ProtoContract]
public class SinkDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the configuration.
    /// </summary>
    [ProtoMember(1)]
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the type of store.
    /// </summary>
    [ProtoMember(2)]
    public string TypeId { get; set; }

    /// <summary>
    /// Gets or sets event-sequence target metadata. Requires the EventSequence sink type, which is not implemented yet.
    /// </summary>
    [ProtoMember(3)]
    public EventSequenceSinkConfiguration? EventSequence { get; set; }

    /// <summary>
    /// Gets or sets the name of the sink configuration to store to. Empty means the default sink for the kind of read model.
    /// </summary>
    /// <remarks>
    /// Kernels that do not know named sinks ignore it, and <see cref="ConfigurationId"/> stays for clients that do not send it.
    /// </remarks>
    [ProtoMember(4)]
    public string Name { get; set; } = string.Empty;
}
