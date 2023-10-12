// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the result of a job step.
/// </summary>
/// <param name="Status">The <see cref="JobStepStatus"/> for the job step.</param>
/// <param name="Messages">Messages associated.</param>
/// <param name="ExceptionStackTrace">Associated exception stack trace, if any.</param>
public record JobStepResult(JobStepStatus Status, IEnumerable<string> Messages, string ExceptionStackTrace)
{
    /// <summary>
    /// Represents a succeeded job step.
    /// </summary>
    public static readonly JobStepResult Succeeded = new(JobStepStatus.Succeeded, Enumerable.Empty<string>(), string.Empty);

    /// <summary>
    /// Gets whether or not the job step was successful.
    /// </summary>
    public bool IsSuccess => Status == JobStepStatus.Succeeded;

    /// <summary>
    /// Creates a failed job step.
    /// </summary>
    /// <param name="message">Message to associate with the failure.</param>
    /// <returns><see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(string message) => new(JobStepStatus.Failed, new[] { message }, string.Empty);
}
