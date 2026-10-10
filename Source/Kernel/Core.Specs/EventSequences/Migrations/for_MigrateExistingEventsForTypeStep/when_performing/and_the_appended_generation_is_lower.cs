// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_the_appended_generation_is_lower : given.the_job_step
{
    readonly Dictionary<EventTypeGeneration, ExpandoObject> _stored = [];
    string _original;
    Catch<JobStepResult> _result;

    void Establish()
    {
        dynamic original = new ExpandoObject();
        original.value = "original with details";
        dynamic latest = new ExpandoObject();
        latest.value = "simplified";
        _stored[1] = original;
        _stored[2] = latest;
        _original = JsonSerializer.Serialize(_stored[1]);
        var eventTypes = _storage.GetEventStore(_jobStepKey.Scope).EventTypes;
        var schema = new JsonSchema();
        eventTypes.GetFor(_eventTypeId, Arg.Any<EventTypeGeneration>()).Returns(call => new EventTypeSchema(new EventType(_eventTypeId, call.ArgAt<EventTypeGeneration>(1)), EventTypeOwner.Server, EventTypeSource.Code, schema));
        eventTypes.GetDefinition(_eventTypeId).Returns(new EventTypeDefinition(_eventTypeId, EventTypeOwner.Server, false, [new(1, schema), new(2, schema), new(3, schema)], []));
        _converter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns((ExpandoObject)original);
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>()).Returns(call =>
            new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = (ExpandoObject)latest, [2] = (ExpandoObject)latest, [3] = (ExpandoObject)latest });
        var sequence = _storage.GetEventStore(_jobStepKey.Scope).GetNamespace(_jobStepKey.Namespace).GetEventSequence(WellKnownEventSequences.EventLog);
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns([new AppendedEvent(EventContext.From(_jobStepKey.Scope, _jobStepKey.Namespace, new EventType(_eventTypeId, 2), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, CorrelationId.NotSet), latest)]);
        cursor.MoveNext().Returns(true, false);
        sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        sequence.GetStoredGenerations(0).Returns(new StoredEventGenerations(0, _eventTypeId, "source", Subject.NotSet, 1, _stored.ToDictionary(_ => _.Key, _ => JsonSerializer.Serialize(_.Value)), 0, string.Empty));
        sequence.TryAddGenerations(Arg.Any<StoredEventGenerations>(), Arg.Any<IEnumerable<GenerationToAdd>>()).Returns(call =>
        {
            foreach (var addition in call.ArgAt<IEnumerable<GenerationToAdd>>(1))
            {
                _stored.Add(addition.Generation, addition.Content);
            }
            return true;
        });
    }

    async Task Because() => _result = await (Task<Catch<JobStepResult>>)typeof(MigrateExistingEventsForTypeStep)
        .GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_jobStep, [new MigrateExistingEventsForTypeStepState { EventTypeId = _eventTypeId }, CancellationToken.None])!;

    [Fact] void should_complete() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_keep_the_appended_content() => JsonSerializer.Serialize(_stored[1]).ShouldEqual(_original);
    [Fact] void should_add_the_missing_generation() => _stored.ContainsKey(3).ShouldBeTrue();
    [Fact] async Task should_not_use_replace_all() => await _storage.GetEventStore(_jobStepKey.Scope).GetNamespace(_jobStepKey.Namespace).GetEventSequence(WellKnownEventSequences.EventLog).DidNotReceive().ReplaceGenerationContent(Arg.Any<EventSequenceNumber>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>());
}
