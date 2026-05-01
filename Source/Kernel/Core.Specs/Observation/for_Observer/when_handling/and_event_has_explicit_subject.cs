// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_has_explicit_subject : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Subject _subject = new("explicit-subject-value");
    string? _capturedIdentifier;
    EventTypeSchema _schema;

    void Establish()
    {
        _schema = new EventTypeSchema(
            event_type,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            new JsonSchema());

        _eventTypesStorage.HasFor(event_type.Id, event_type.Generation).Returns(true);
        _eventTypesStorage.GetFor(event_type.Id, event_type.Generation).Returns(_schema);
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
        _complianceManager
            .When(m => m.Release(
                Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(),
                Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>()))
            .Do(callInfo => _capturedIdentifier = callInfo.ArgAt<string>(3));
        _complianceManager.Release(
            Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(),
            Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<string>(),
            Arg.Any<JsonObject>())
            .Returns(new JsonObject());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(new ExpandoObject());

        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(42UL));
    }

    async Task Because()
    {
        var eventWithSubject = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL).Context with
            {
                Subject = _subject
            }
        };
        await _observer.Handle("Something", [eventWithSubject]);
    }

    [Fact] void should_use_subject_value_as_identifier() => _capturedIdentifier.ShouldEqual(_subject.Value);
}
