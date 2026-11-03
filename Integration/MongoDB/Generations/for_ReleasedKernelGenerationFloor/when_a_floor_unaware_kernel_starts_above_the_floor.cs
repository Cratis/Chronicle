// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// Characterizes the hazard the startup floor cannot remove: a kernel released before floor admission (v19.106.2) ignores
/// the durable floor, starts, and writes.
/// </summary>
/// <remarks>
/// The floor cannot exclude binaries released before v19.106.6; exclusion for them is the remainder of
/// https://github.com/Cratis/Chronicle/issues/5226. This spec exists so that a future barrier has a failing baseline.
/// </remarks>
public class when_a_floor_unaware_kernel_starts_above_the_floor() : given.a_populated_store(ReleasedImages.FloorUnaware, seedFloor: true, expectRefusal: false)
{
    [Fact] void should_have_seeded_the_floor_in_the_stores_the_kernel_populated() => _outcome.SeededStores.ShouldBeGreaterThan(0);
    [Fact] void should_start_despite_the_floor() => _outcome.Exited.ShouldBeFalse();
    [Fact] void should_write_to_an_event_store_database() => _outcome.Writes.ShouldBeGreaterThan(0L);
}
