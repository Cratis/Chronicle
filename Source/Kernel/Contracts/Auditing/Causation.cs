// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Aksio.Cratis.Kernel.Contracts.Primitives;

namespace Aksio.Cratis.Kernel.Contracts.Auditing;

/// <summary>
/// Represents the payload for causation.
/// </summary>
[DataContract]
public class Causation
{
    /// <summary>
    /// Gets or sets the time and date for when it occurred.
    /// </summary>
    [DataMember(Order = 1)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the type of cause.
    /// </summary>
    [DataMember(Order = 2)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets properties associated with the causation.
    /// </summary>
    [DataMember(Order = 3)]
    public IDictionary<string, string> Properties { get; set; }
}
