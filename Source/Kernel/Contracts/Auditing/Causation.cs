// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Kernel.Contracts.Auditing;

/// <summary>
/// Represents the payload for causation.
/// </summary>
[ProtoContract]
public class Causation
{
    /// <summary>
    /// Gets or sets the time and date for when it occurred.
    /// </summary>
    [ProtoMember(1)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the type of cause.
    /// </summary>
    [ProtoMember(2)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets properties associated with the causation.
    /// </summary>
    [ProtoMember(3)]
    public IDictionary<string, string> Properties { get; set; }
}
