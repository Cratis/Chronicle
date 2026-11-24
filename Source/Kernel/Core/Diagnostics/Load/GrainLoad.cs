// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// What this silo's grain calls look like for one grain type, and one event store where the grain's key carries one.
/// </summary>
/// <param name="GrainType">The name of the grain interface the calls are made to.</param>
/// <param name="EventStore">The event store the grains belong to, or an empty string when the grain's key does not carry one.</param>
/// <param name="Running">How many calls to grains of the type are running on this silo, interleaved or not.</param>
/// <param name="Pending">How many calls to grains of the type this silo has made that have not been answered: running or queued at the grain.</param>
/// <param name="OldestPendingAge">How long the oldest of the pending calls has been waiting.</param>
public record GrainLoad(string GrainType, string EventStore, int Running, int Pending, TimeSpan OldestPendingAge);
