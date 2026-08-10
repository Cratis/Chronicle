// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventStoreSeedingExtensions;

public class when_the_event_store_uses_an_alternative_seeding_implementation : Specification
{
    IEventStore _eventStore;
    Exception _error;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Seeding.Returns(Substitute.For<IEventSeeding>());
    }

    void Because() => _error = Catch.Exception(() => _eventStore.CreateEventSeeding());

    [Fact] void should_explain_that_an_independent_buffer_cannot_be_created() =>
        _error.ShouldBeOfExactType<CannotCreateEventSeeding>();
}
