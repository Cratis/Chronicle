// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// A released kernel that supports floor admission (v19.106.6) must refuse to start on stores whose durable floor is above
/// its support, before it changes any event-store database. Its non-vacuity sibling is
/// <see cref="when_a_floor_aware_kernel_starts_without_a_floor"/>, which records the writes this one must not make.
/// </summary>
public class when_a_floor_aware_kernel_starts_below_the_floor() : given.a_populated_store(ReleasedImages.FloorAware, seedFloor: true, expectRefusal: true)
{
    [Fact] void should_have_seeded_the_floor_in_the_stores_the_kernel_populated() => _outcome.SeededStores.ShouldBeGreaterThan(0);
    [Fact] void should_exit_instead_of_becoming_healthy() => _outcome.Exited.ShouldBeTrue();
    [Fact] void should_report_that_the_kernel_is_below_the_generation_floor() => _outcome.NamedTheFloor.ShouldBeTrue();
    [Fact] void should_not_write_to_any_event_store_database() => _outcome.Writes.ShouldEqual(0);
    [Fact] void should_not_create_event_store_databases() => _outcome.CreatedDatabases.ShouldEqual(0);
}
