// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// What a silo's calls look like for one grain type, and one event store where the grain's key carries one.
/// </summary>
/// <param name="GrainType">The name of the grain interface.</param>
/// <param name="EventStore">The event store the grains belong to, or an empty string when the grain's key does not carry one.</param>
/// <param name="Running">How many calls are running on grains of the type, interleaved or not.</param>
/// <param name="Pending">How many calls the silo has made to grains of the type that have not been answered.</param>
/// <param name="OldestPendingAgeSeconds">How long the oldest of the pending calls has been waiting, in seconds.</param>
public record BusiestGrain(string GrainType, string EventStore, int Running, int Pending, double OldestPendingAgeSeconds);
