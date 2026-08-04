// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_getting_the_event_type_for;

/// <summary>
/// Every model-bound projection attribute takes an unconstrained generic type argument, so nothing at compile time
/// relates it to an event type and this resolution is the first place the mistake can be caught. Answering it with
/// a LINQ exception - "Sequence contains no matching element", naming neither the type nor what is wrong with it -
/// leaves a consumer with nothing to act on. The fluent projection builder already reports the same condition as
/// <see cref="TypeIsNotAnEventType"/>; this makes every other caller say the same thing.
/// </summary>
public class a_clr_type_that_is_not_a_registered_event_type : given.all_dependencies
{
    [EventType]
    record RegisteredEvent(string Message);

    record UnregisteredEvent(string Message);

    EventTypes _subject;
    Exception _error;

    async Task Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(RegisteredEvent)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
        await _subject.Discover();
    }

    void Because() => _error = Catch.Exception(() => _subject.GetEventTypeFor(typeof(UnregisteredEvent)));

    [Fact] void should_report_that_the_type_is_not_an_event_type() => _error.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_name_the_type_that_could_not_be_resolved() => _error.Message.ShouldContain(nameof(UnregisteredEvent));
}
