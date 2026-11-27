// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_grouped_entries;

public class and_serializing_an_entry_fails : given.a_seeding_builder_that_records_serialization
{
    Exception _error;

    void Establish()
    {
        SerializeFailsFor("b1");
        _seeding.ForEventSource("s1", [new Alpha("a1"), new Beta("b1"), new Alpha("a2")]);
    }

    async Task Because() => _error = await Catch.Exception(_seeding.Register);

    [Fact] void should_fail_with_the_serialization_error() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_stop_serializing_at_the_failing_entry() => _serialized.ShouldEqual(["a1", "b1"]);
    [Fact] async Task should_not_send_any_seed_entries() => await _seedingService.DidNotReceive().SeedEvents(Arg.Any<SeedEventsRequest>());
}
