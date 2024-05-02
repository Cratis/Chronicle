// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Jobs;

/// <summary>
/// Represents a status change event that occurred for a job step.
/// </summary>
/// <param name="Status"><see cref="JobStatus"/> it changed to.</param>
/// <param name="Occurred">When it occurred.</param>
/// <param name="ExceptionMessages">Any exception messages.</param>
/// <param name="ExceptionStackTrace">Exception stack trace, if errored.</param>
public record JobStepStatusChanged(JobStepStatus Status, DateTimeOffset Occurred, IEnumerable<string> ExceptionMessages, string ExceptionStackTrace);
