// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// The exception that is thrown when a replay job is resumed but its observer does not enter replay with it.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
/// <param name="jobId">The <see cref="JobId"/> of the replay job being resumed.</param>
/// <param name="replayJobId">The <see cref="JobId"/> the observer reported replaying with.</param>
public class ObserverDidNotEnterReplay(ObserverKey observerKey, JobId jobId, JobId replayJobId)
    : Exception($"Observer '{observerKey}' did not enter replay with replay job '{jobId}' (it reported '{replayJobId}'), so the job was not resumed");
