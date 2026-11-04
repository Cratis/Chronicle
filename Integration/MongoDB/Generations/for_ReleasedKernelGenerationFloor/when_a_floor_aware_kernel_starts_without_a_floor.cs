// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// Non-vacuity for <see cref="when_a_floor_aware_kernel_starts_below_the_floor"/>: the same binary, image, stores and write
/// witness, with no floor seeded. The witness must see the writes whose absence the refusal spec asserts.
/// </summary>
public class when_a_floor_aware_kernel_starts_without_a_floor() : given.a_populated_store(ReleasedImages.FloorAware, seedFloor: false, expectRefusal: false)
{
    [Fact] void should_write_to_an_event_store_database() => _outcome.Writes.ShouldBeGreaterThan(0L);
}
