// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents the result of an observation.
/// </summary>
[ProtoContract]
public class ObservationResult
{
    /// <summary>
    /// Gets or sets the state of the observer.
    /// </summary>
    [ProtoMember(1)]
    public ObservationState State { get; set; }

    /// <summary>
    /// Gets or sets the last successful observation.
    /// </summary>
    [ProtoMember(2)]
    public ulong LastSuccessfulObservation { get; set; }

    /// <summary>
    /// Gets or sets the exception messages.
    /// </summary>
    [ProtoMember(3)]
    public IList<string> ExceptionMessages { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the exception stack trace.
    /// </summary>
    [ProtoMember(4)]
    public string ExceptionStackTrace { get; set; }
}
