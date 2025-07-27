// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents a state change event.
/// </summary>
[ProtoContract]
public class JobStepStatusChanged
{
    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    [ProtoMember(1)]
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    [ProtoMember(2)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets any exception messages that happened during the job step - typically when it failed.
    /// </summary>
    [ProtoMember(3)]
    public IEnumerable<string> ExceptionMessages { get; set; }

    /// <summary>
    /// Gets or sets the stack trace for the exception that happened during the job step - typically when it failed.
    /// </summary>
    [ProtoMember(4)]
    public string ExceptionStackTrace { get; set; }
}
