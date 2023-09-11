// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Aksio.Cratis.Kernel.Contracts.Auditing;
using Aksio.Cratis.Kernel.Contracts.Identities;
using Aksio.Cratis.Kernel.Contracts.Primitives;

namespace Aksio.Cratis.Kernel.Contracts.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
[DataContract]
public class AppendRequest
{
    /// <summary>
    /// Gets or sets the microservice identifier.
    /// </summary>
    [DataMember(Order = 1)]
    public string MicroserviceId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [DataMember(Order = 2)]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [DataMember(Order = 3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [DataMember(Order = 4)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [DataMember(Order = 5)]
    public string EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event - in the form of a JSON payload.
    /// </summary>
    [DataMember(Order = 6)]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the causation.
    /// </summary>
    [DataMember(Order = 7)]
    public IEnumerable<Causation> Causation { get; set; }

    /// <summary>
    /// Gets or sets the caused by.
    /// </summary>
    [DataMember(Order = 8)]
    public Identity Identity { get; set; }

    /// <summary>
    /// Gets or sets the valid from.
    /// </summary>
    [DataMember(Order = 9)]
    public SerializableDateTimeOffset? ValidFrom { get; set; }
}
