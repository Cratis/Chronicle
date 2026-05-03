// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Regression spec: events stored before the Subject field was introduced have Subject = null.
/// DecryptEvents must fall back to EventSourceId instead of throwing NullReferenceException.
/// </summary>
public class and_event_has_null_subject : given.an_observer_with_subscription_and_schema_for_event_type
{
    string? _capturedIdentifier;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
        _complianceManager
            .When(m => m.Release(
                Arg.Any<Concepts.EventStoreName>(),
                Arg.Any<Concepts.EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>()))
            .Do(callInfo => _capturedIdentifier = callInfo.ArgAt<string>(3));
        _complianceManager.Release(
            Arg.Any<Concepts.EventStoreName>(),
            Arg.Any<Concepts.EventStoreNamespaceName>(),
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
        var eventWithNullSubject = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL).Context with
            {
                EventSourceId = _eventSourceId,
                Subject = null!
            }
        };
        await _observer.Handle(_eventSourceId, [eventWithNullSubject]);
    }

    [Fact] void should_not_throw() => true.ShouldBeTrue();
    [Fact] void should_fall_back_to_event_source_id_as_identifier() => _capturedIdentifier.ShouldEqual(_eventSourceId.Value);
}
