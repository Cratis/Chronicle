// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents the result of a job step.
/// </summary>
/// <param name="Status">The <see cref="JobStepStatus"/> for the job step.</param>
/// <param name="Messages">Messages associated.</param>
/// <param name="ExceptionStackTrace">Associated exception stack trace, if any.</param>
/// <param name="Result">Result of the job step.</param>
public record JobStepResult(JobStepStatus Status, IEnumerable<string> Messages, string ExceptionStackTrace, object? Result = null)
{
    /// <summary>
    /// Gets whether or not the job step was successful.
    /// </summary>
    public bool IsSuccess => Status == JobStepStatus.Succeeded;

    /// <summary>
    /// Create a succeeded job step with optional result object.
    /// </summary>
    /// <param name="result">Optional result object.</param>
    /// <returns>A new <see cref="JobStepResult"/> instance.</returns>
    public static JobStepResult Succeeded(object? result = null) => new(JobStepStatus.Succeeded, [], string.Empty, result);

    /// <summary>
    /// Creates a failed job step.
    /// </summary>
    /// <param name="message">Message to associate with the failure.</param>
    /// <returns><see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(string message) => new(JobStepStatus.Failed, [message], string.Empty);
}
