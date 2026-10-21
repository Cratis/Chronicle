// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_discovering_handlers : given.an_occurrence_client
{
    void Establish() => _artifacts.OccurrenceHandlers.Returns([typeof(given.TestOccurrenceHandler)]);

    void Because() => _registry.Discover();

    [Fact] void should_resolve_the_typed_trigger() => _registry.Handlers.Single().Trigger.ShouldEqual(typeof(given.TestTrigger));
    [Fact] void should_resolve_the_handler() => _registry.Handlers.Single().Handler.ShouldEqual(typeof(given.TestOccurrenceHandler));
}
