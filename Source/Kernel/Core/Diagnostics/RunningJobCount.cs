// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// How many observer jobs of a type run on a silo for an event store and namespace.
/// </summary>
/// <param name="EventStore">The event store the jobs are for.</param>
/// <param name="Namespace">The namespace the jobs are for.</param>
/// <param name="JobType">The type of the jobs: <c language="csharp">catch_up</c> or <c language="csharp">replay</c>.</param>
/// <param name="Count">How many jobs are running.</param>
public record RunningJobCount(string EventStore, string Namespace, string JobType, int Count);
