// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Converts stored job step state into the job step summary read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class JobStepSummaryConverters
{
    /// <summary>
    /// Converts a stored job step state into a job step summary.
    /// </summary>
    /// <param name="step">The stored job step state.</param>
    /// <returns>The job step summary.</returns>
    internal static JobStepSummary ToJobStep(JobStepState step) =>
        new(
            step.Id.JobStepId,
            step.Type,
            step.Name,
            (JobStepStatus)(int)step.Status,
            step.StatusChanges.Select(ToStatusChanged),
            ToProgress(step.Progress));

    /// <summary>
    /// Converts a stored status change into its contract representation.
    /// </summary>
    /// <param name="sc">The stored status change.</param>
    /// <returns>The status change.</returns>
    internal static JobStepStatusChanged ToStatusChanged(Concepts.Jobs.JobStepStatusChanged sc) =>
        new()
        {
            Status = (JobStepStatus)(int)sc.Status,
            Occurred = sc.Occurred,
            ExceptionMessages = sc.ExceptionMessages.ToList(),
            ExceptionStackTrace = sc.ExceptionStackTrace
        };

    /// <summary>
    /// Converts stored progress into its contract representation.
    /// </summary>
    /// <param name="p">The stored progress.</param>
    /// <returns>The progress.</returns>
    internal static JobStepProgress ToProgress(Concepts.Jobs.JobStepProgress p) =>
        new()
        {
            Percentage = (int)p.Percentage,
            Message = (string)p.Message
        };
}
