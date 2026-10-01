// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Owns the publication identity of full reducer replays for one observer.
/// </summary>
public interface IReducerReplay : IGrainWithStringKey
{
    /// <summary>
    /// Start a fresh attempt, fencing every previous attempt from publication.
    /// </summary>
    /// <param name="jobId">The owning job.</param>
    /// <returns>The isolated target carried by every batch.</returns>
    Task<ReplayContext> Begin(JobId jobId);

    /// <summary>
    /// Publish a successfully rebuilt target, if it still owns the replay.
    /// </summary>
    /// <param name="context">The completed attempt.</param>
    /// <returns>The publication outcome, separate from bookkeeping.</returns>
    Task<ReplayPublication> Publish(ReplayContext context);

    /// <summary>
    /// Revoke an abandoned job's right to publish without touching another attempt.
    /// </summary>
    /// <param name="jobId">The abandoned job.</param>
    /// <returns>Awaitable task.</returns>
    Task Abandon(JobId jobId);
}
