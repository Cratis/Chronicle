// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class an_event_seeding_grain : Specification
{
    protected EventSeeding _grain;
    protected IPersistentState<EventSeeds> _state;
    protected IEventSequence _eventSequence;
    protected EventSeedingKey _key;
    protected ILogger<EventSeeding> _logger;

    void Establish()
    {
        _key = new EventSeedingKey("TestEventStore", "TestNamespace");
        _state = Substitute.For<IPersistentState<EventSeeds>>();
        _eventSequence = Substitute.For<IEventSequence>();
        _logger = Substitute.For<ILogger<EventSeeding>>();

        _state.State.Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>()));

        _grain = new EventSeeding(_state, _logger);

        // Simulate OnActivateAsync by setting internal fields via reflection
        var keyField = typeof(EventSeeding).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        keyField.SetValue(_grain, _key);

        var eventSequenceField = typeof(EventSeeding).GetField("_eventSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        eventSequenceField.SetValue(_grain, _eventSequence);
    }
}
