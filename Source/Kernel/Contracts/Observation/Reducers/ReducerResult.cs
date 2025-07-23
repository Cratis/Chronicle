// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the result of an observation.
/// </summary>
[ProtoContract]
public class ReducerResult
{
    /// <summary>
    /// Gets or sets the partition that was observed.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets the state of the observer.
    /// </summary>
    [ProtoMember(2)]
    public ObservationState State { get; set; }

    /// <summary>
    /// Gets or sets the last successful observation.
    /// </summary>
    [ProtoMember(3)]
    public ulong LastSuccessfulObservation { get; set; }

    /// <summary>
    /// Gets or sets the exception messages.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the exception stack trace.
    /// </summary>
    [ProtoMember(5)]
    public string ExceptionStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the resulting state.
    /// </summary>
    [ProtoMember(6)]
    public string? ModelState { get; set; }
}
