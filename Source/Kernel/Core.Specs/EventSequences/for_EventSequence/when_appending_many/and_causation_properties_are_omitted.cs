// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_causation_properties_are_omitted : given.appending_many_events
{
    Causation _ambient;
    Causation _individual;
    EventToAppendToStorage[] _storedEvents;

    protected override CausationPropertyRetention CausationPropertyRetention => CausationPropertyRetention.Omit;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string> { ["name"] = "personal value" });
        _individual = new(DateTimeOffset.UnixEpoch.AddMinutes(1), "individual", new Dictionary<string, string> { ["name"] = "another value" });
        _events =
        [
            EventToAppendFor("first") with { Causation = [_ambient, _individual] },
            EventToAppendFor("second")
        ];
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _storedEvents = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(_storedEvents)));
            });
    }

    async Task Because() => await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [_ambient],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_omit_individual_causation_properties() => _storedEvents[0].Causation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_omit_shared_causation_properties() => _storedEvents[1].Causation.Single().Properties.ShouldBeEmpty();
    [Fact] void should_keep_per_event_types_and_times() => _storedEvents[0].Causation.Select(causation => (causation.Type, causation.Occurred)).ShouldEqual([(_ambient.Type, _ambient.Occurred), (_individual.Type, _individual.Occurred)]);
    [Fact] void should_not_mutate_shared_causation() => _ambient.Properties["name"].ShouldEqual("personal value");
    [Fact] void should_not_mutate_per_event_causation() => _individual.Properties["name"].ShouldEqual("another value");
}
