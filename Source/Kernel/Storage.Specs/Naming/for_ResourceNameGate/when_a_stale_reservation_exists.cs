// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_a_stale_reservation_exists : given.a_gate
{
    ResourceNameConflict _variant;
    Exception _owner;

    void Establish()
    {
        _snapshot.Publish([], []);
        var held = new FakeReservations();
        held.Reserve("orders", ["store:ORDERS"]).GetAwaiter().GetResult();
        _gate = new(_snapshot, held, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
    }

    async Task Because()
    {
        _variant = (ResourceNameConflict)await Catch.Exception(() => _gate.EnsureCanCreate(Store("Orders")));
        _owner = await Catch.Exception(() => _gate.EnsureCanCreate(Store("orders")));
    }

    [Fact] void should_block_a_case_variant() => _variant.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_not_block_its_own_owner() => _owner.ShouldBeNull();
}
