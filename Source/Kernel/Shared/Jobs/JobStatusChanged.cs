// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents a status change event that occurred for a job.
/// </summary>
public class JobStatusChanged
{
    /// <summary>
    /// Gets or sets the <see cref="JobStatus"/>.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets any exception messages that happened during the job step - typically when it failed.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets or sets the stack trace for the exception that happened during the job step - typically when it failed.
    /// </summary>
    public string ExceptionStackTrace { get; set; } = string.Empty;
}
