// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

/// <summary>
/// Models the event shape SQL storage produced before default reads delivered the revision (#4751): a context
/// generation above the revised generation. SQL storage now reads a revised event at its revision's generation,
/// so this documents the release logic only, not current SQL read behavior.
/// </summary>
public class and_sql_records_a_revision_below_the_context_generation : given.a_release_boundary
{
    EventType _webhookPin;
    AppendedEvent[] _observerDeliveries;
    AppendedEvent[] _webhookDeliveries;
    AppendedEvent[] _deliveriesWithoutRevisedGeneration;
    AppendedEvent _eventWithoutRevisedGeneration;
    AppendedEvent _emptyLatestRevision;
    AppendedEvent _mismatchedLatestRevision;
    Exception _emptyLatestRevisionError;
    Exception _mismatchedLatestRevisionError;

    async Task Establish()
    {
        _event = _event with
        {
            Context = _event.Context with { EventType = _pin },
            RevisedGeneration = 1,
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"revised-name\"}", [2] = "{\"fullName\":\"old-name\"}" }
        };
        _webhookPin = new(_pin.Id, 1);
        _schemas[_webhookPin] = new(_webhookPin, EventTypeOwner.Client, EventTypeSource.Code, _firstSchema);
        _migrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(call =>
            {
                var content = (JsonObject)call[2];
                var value = content[0]!.GetValue<string>();
                return new Dictionary<EventTypeGeneration, ExpandoObject>
                {
                    [1] = _converter.ToExpandoObject(new JsonObject { ["name"] = value }, _firstSchema),
                    [2] = _converter.ToExpandoObject(new JsonObject { ["fullName"] = value }, _schema)
                };
            });
        var revised = _event with
        {
            Context = _event.Context with { EventType = _webhookPin },
            Content = _converter.ToExpandoObject(new JsonObject { ["name"] = "New" }, _firstSchema),
            OriginalContent = "{\"name\":\"Old\"}",
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"Old\"}", [2] = "{\"fullName\":\"Old\"}" },
            Revisions = [new(1, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred, "{\"name\":\"New\"}")]
        };
        _eventWithoutRevisedGeneration = revised with { RevisedGeneration = null };
        var revisedTwice = revised with
        {
            Revisions = [new(1, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred, "{\"name\":\"Earlier\"}"), .. revised.Revisions]
        };
        _observerDeliveries = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [revised, revisedTwice]);
        var latestRevision = revisedTwice.Revisions.Last();
        _emptyLatestRevision = _observerDeliveries[1] with
        {
            Revisions = [.. revisedTwice.Revisions, latestRevision with { Content = string.Empty }]
        };
        _mismatchedLatestRevision = _observerDeliveries[1] with
        {
            Revisions = [.. revisedTwice.Revisions, latestRevision with { EventTypeGeneration = 2, Content = "{\"fullName\":\"Unexpected\"}" }]
        };
    }

    async Task Because()
    {
        _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);
        _webhookDeliveries = await _release.Release(_event.Context.EventStore, [_webhookPin], _schemas, _observerDeliveries);
        _deliveriesWithoutRevisedGeneration = await _release.Release(_event.Context.EventStore, [_webhookPin], _schemas, [_eventWithoutRevisedGeneration]);
        _emptyLatestRevisionError = await Catch.Exception(() => _release.Release(_event.Context.EventStore, [_webhookPin], _schemas, [_emptyLatestRevision]));
        _mismatchedLatestRevisionError = await Catch.Exception(() => _release.Release(_event.Context.EventStore, [_webhookPin], _schemas, [_mismatchedLatestRevision]));
    }

    [Fact] void should_migrate_the_revision_instead_of_using_the_context_generation() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("revised-name");
    [Fact] void should_deliver_the_single_revision_to_the_observer() => ((IDictionary<string, object?>)_observerDeliveries[0].Content)["fullName"].ShouldEqual("New");
    [Fact] void should_deliver_the_latest_revision_to_the_observer() => ((IDictionary<string, object?>)_observerDeliveries[1].Content)["fullName"].ShouldEqual("New");
    [Fact] void should_repin_the_single_revision_instead_of_the_original() => ((IDictionary<string, object?>)_webhookDeliveries[0].Content)["name"].ShouldEqual("New");
    [Fact] void should_repin_the_latest_revision_instead_of_the_original_or_earlier_revision() => ((IDictionary<string, object?>)_webhookDeliveries[1].Content)["name"].ShouldEqual("New");
    [Fact] void should_use_the_webhook_pin() => _webhookDeliveries.ShouldEachConformTo(_ => _.Context.EventType == _webhookPin);
    [Fact] void should_deliver_from_the_context_generation_without_an_explicit_revised_generation() => _deliveriesWithoutRevisedGeneration.ShouldEachConformTo(_ => _.Context.EventType == _webhookPin && (string)((IDictionary<string, object?>)_.Content)["name"]! == "New");
    [Fact] void should_preserve_the_appended_originals() => _webhookDeliveries.ShouldEachConformTo(_ => _.GenerationalContent[1] == "{\"name\":\"Old\"}");
    [Fact] void should_preserve_the_generation_two_originals() => _webhookDeliveries.ShouldEachConformTo(_ => _.GenerationalContent[2] == "{\"fullName\":\"Old\"}");
    [Fact] void should_reject_empty_latest_revision_content_instead_of_resurrecting_an_original_or_earlier_revision() => _emptyLatestRevisionError.ShouldBeOfExactType<PinnedEventTypeGenerationUnavailable>();
    [Fact] void should_reject_a_mismatched_latest_revision_generation_instead_of_resurrecting_an_original_or_earlier_revision() => _mismatchedLatestRevisionError.ShouldBeOfExactType<PinnedEventTypeGenerationUnavailable>();
}
