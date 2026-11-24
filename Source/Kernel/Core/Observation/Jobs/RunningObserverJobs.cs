// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// How many observer jobs of a type run for an event store and namespace.
/// </summary>
/// <param name="EventStore">The event store the jobs are for.</param>
/// <param name="Namespace">The namespace the jobs are for.</param>
/// <param name="JobType">The type of the jobs, in lowercase.</param>
/// <param name="Count">How many jobs are running.</param>
public record RunningObserverJobs(string EventStore, string Namespace, string JobType, int Count);
