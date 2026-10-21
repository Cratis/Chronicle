// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the outcome of asking a job to stop.
/// </summary>
/// <param name="Accepted">Whether the job took the stop.</param>
/// <param name="Refusal">The reason the stop was refused, <see cref="JobCommandRefusal.NotRefused"/> when it was accepted.</param>
public record StopJobOutcome(bool Accepted, JobCommandRefusal Refusal);
