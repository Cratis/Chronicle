// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Translates what the jobs manager answered into the outcome handed back to the caller.
/// </summary>
internal static class JobCommandOutcomes
{
    /// <summary>
    /// Translates the answer to a stop.
    /// </summary>
    /// <param name="result">What the jobs manager answered.</param>
    /// <returns>The <see cref="StopJobOutcome"/>.</returns>
    internal static StopJobOutcome ForStop(Result<StopJobError> result) =>
        result.TryGetError(out var error) ? new(false, RefusalFor(error)) : new(true, JobCommandRefusal.NotRefused);

    /// <summary>
    /// Translates the answer to a resume.
    /// </summary>
    /// <param name="taken">Whether anything took the job forward.</param>
    /// <returns>The <see cref="ResumeJobOutcome"/>.</returns>
    internal static ResumeJobOutcome ForResume(bool taken) =>
        taken ? new(true, JobCommandRefusal.NotRefused) : new(false, JobCommandRefusal.NotResumed);

    static JobCommandRefusal RefusalFor(StopJobError error) => error switch
    {
        StopJobError.JobIsCompleted => JobCommandRefusal.JobIsCompleted,
        StopJobError.JobIsNotRunning => JobCommandRefusal.JobIsNotRunning,
        StopJobError.CouldNotPersistStop => JobCommandRefusal.CouldNotPersist,
        _ => JobCommandRefusal.NotFound
    };
}
