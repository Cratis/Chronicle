// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_enum_leaves_are_encrypted : given.an_event_sequence_with_a_compliant_enum
{
    AppendManyResult _result;
    EventToAppendToStorage[] _storedEvents;
    AppendedEvent _releasedFirstEvent;

    void Establish() =>
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                _storedEvents = callInfo.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                var appended = _storedEvents.Select(eventToAppend => new AppendedEvent(
                    EventContext.From(
                        EventStore,
                        EventStoreNamespace,
                        eventToAppend.EventType,
                        eventToAppend.EventSourceType,
                        eventToAppend.EventSourceId,
                        eventToAppend.EventStreamType,
                        eventToAppend.EventStreamId,
                        eventToAppend.SequenceNumber,
                        eventToAppend.CorrelationId),
                    new ExpandoObject()));
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(appended));
            });

    async Task Because()
    {
        var events = new[]
        {
            new EventToAppend(EventSourceType.Default, "source-1", EventStreamType.All, EventStreamId.Default, _eventType, [], ValidContent()),
            new EventToAppend(EventSourceType.Default, "source-2", EventStreamType.All, EventStreamId.Default, _eventType, [], new() { ["status"] = 1 })
        };

        _result = await _eventSequence.AppendMany(
            events,
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

        var compliance = new EventCompliance(_realComplianceManager, _realConverter);
        _releasedFirstEvent = await compliance.Release(
            new AppendedEvent(
                EventContext.From(
                    EventStore,
                    EventStoreNamespace,
                    _eventType,
                    EventSourceType.Default,
                    "source-1",
                    EventStreamType.All,
                    EventStreamId.Default,
                    EventSequenceNumber.First,
                    CorrelationId.NotSet,
                    subject: "source-1"),
                _storedEvents[0].Content),
            _compliantEnumSchema);
    }

    [Fact] void should_append_the_batch_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_events() => _storedEvents.Length.ShouldEqual(2);
    [Fact] void should_keep_the_first_ciphertext_opaque() => ContentOf(0)["status"].ShouldBeOfExactType<string>();
    [Fact] void should_keep_the_second_ciphertext_opaque() => ContentOf(1)["status"].ShouldBeOfExactType<string>();
    [Fact] void should_not_store_the_first_plaintext_value() => ContentOf(0)["status"].ShouldNotEqual("0");
    [Fact] void should_not_store_the_second_plaintext_value() => ContentOf(1)["status"].ShouldNotEqual("1");
    [Fact] void should_release_the_original_zero_enum_value() => ((IDictionary<string, object?>)_releasedFirstEvent.Content)["status"].ShouldEqual(0);

    IDictionary<string, object?> ContentOf(int index) => _storedEvents[index].Content;
}
