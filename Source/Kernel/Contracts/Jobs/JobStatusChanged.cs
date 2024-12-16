// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents a status change event that occurred for a job.
/// </summary>
[ProtoContract]
public class JobStatusChanged
{
    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    [ProtoMember(1)]
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    [ProtoMember(2)]
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets any exception messages that happened during the job step - typically when it failed.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the stack trace for the exception that happened during the job step - typically when it failed.
    /// </summary>
    [ProtoMember(4)]
    public string ExceptionStackTrace { get; set; } = string.Empty;
}
