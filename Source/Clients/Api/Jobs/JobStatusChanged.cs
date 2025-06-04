// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the status change of a job.
/// </summary>
/// <param name="Status">The status of the job.</param>
/// <param name="Occurred">When the status change occurred.</param>
/// <param name="ExceptionMessages">The exception messages that occurred.</param>
/// <param name="ExceptionStackTrace">The exception stack traces that occurred.</param>
public record JobStatusChanged(
    JobStatus Status,
    DateTimeOffset Occurred,
    IEnumerable<string> ExceptionMessages,
    string ExceptionStackTrace);
