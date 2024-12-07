// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents the result of performing a job step.
/// </summary>
public class JobStepResult : Result<object?, PerformJobStepError>
{
    JobStepResult(Result<object?, PerformJobStepError> input)
        : base(input)
    {
    }

    /// <summary>
    /// Creates a succeeded <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="input">The optional result object.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Succeeded(object? input = default) => new(Result.Success<object?, PerformJobStepError>(input));

    /// <summary>
    /// Creates a failed <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="input">The error messages.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static new JobStepResult Failed(PerformJobStepError input) => new(input);

    /// <summary>
    /// Creates a failed <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="messages">The error messages.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(params string[] messages) => new(new PerformJobStepError(default, messages, string.Empty));

    /// <summary>
    /// Creates a failed <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(Exception ex) => new(new PerformJobStepError(default, ex.GetAllMessages(), ex.StackTrace));
}
