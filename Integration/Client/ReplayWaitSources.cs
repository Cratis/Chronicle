// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using ProjectionState = Cratis.Chronicle.Projections.ProjectionState;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Represents the state sources and known observer identities read when a replay wait fails.
/// </summary>
/// <param name="GetJobs">Reads the jobs.</param>
/// <param name="ReactorName">The known reactor identity.</param>
/// <param name="GetReactorState">Reads the reactor state.</param>
/// <param name="ReducerName">The known reducer identity.</param>
/// <param name="GetReducerState">Reads the reducer state.</param>
/// <param name="ProjectionName">The known projection identity.</param>
/// <param name="GetProjectionState">Reads the projection state.</param>
/// <param name="GetJobScope">Reads the observer and partition from a job's typed request, if available.</param>
/// <param name="GetJobSteps">Overrides the job step reader.</param>
/// <param name="ScenarioSource">The scenario's event source, not necessarily the failed job's partition.</param>
internal sealed record ReplayWaitSources(
    Func<Task<IEnumerable<Job>>> GetJobs,
    string ReactorName,
    Func<Task<ReactorState>> GetReactorState,
    string ReducerName,
    Func<Task<ReducerState>> GetReducerState,
    string ProjectionName,
    Func<Task<ProjectionState>> GetProjectionState,
    Func<JobId, Task<(string Observer, string Partition)?>>? GetJobScope = null,
    Func<Job, Task<IEnumerable<JobStep>>>? GetJobSteps = null,
    string? ScenarioSource = null);
