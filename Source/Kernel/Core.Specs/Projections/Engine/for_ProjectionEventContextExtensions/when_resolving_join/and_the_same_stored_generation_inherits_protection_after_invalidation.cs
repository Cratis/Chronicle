// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Events.for_EventCompliance.given;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_same_stored_generation_inherits_protection_after_invalidation : a_stored_generation_with_inherited_protection
{
    System.Reactive.Subjects.Subject<ProjectionEventContext> _subject;
    IEventSequenceStorage _sequenceStorage;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    readonly List<ProjectionEventContext> _results = [];
    JsonObject _stored;
    JsonObject _released;

    async Task Establish()
    {
        // The projection and its retained schema predate the later generation's classification.
        var currentDefinition = _definition;
        _definition = _definition with { Generations = [new(1, _own)] };
        await _cache.GetProtectionSchemaFor("store", _type.Id, _type.Generation, _own);
        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        dynamic state = new ExpandoObject();
        state.joinId = "source";
        _changeset.CurrentState.Returns((ExpandoObject)state);
        _changeset.ResolvedJoin(Arg.Any<PropertyPath>(), Arg.Any<object>(), Arg.Any<AppendedEvent>(), Arg.Any<ArrayIndexers>()).Returns(_changeset);
        _subject = new();
        _subject.ResolveJoin(_sequenceStorage, _type, new PropertyPath("joinId"), NullLogger.Instance, _compliance, _own).Subscribe(_results.Add);
        _definition = currentDefinition;
        _cache.Invalidate("store", _type.Id);
    }

    async Task Because()
    {
        var effective = await _cache.GetProtectionSchemaFor("store", _type.Id, _type.Generation, _own);
        _stored = await _manager.Apply("store", "namespace", effective, Identifier, _plaintext);
        var storedEvent = EventWith(_stored);
        _sequenceStorage.TryGetLastEventBefore(_type.Id, "source", Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(storedEvent))));
        _subject.OnNext(new(new("source", ArrayIndexers.NoIndexers), EventWith(_plaintext) with { Context = storedEvent.Context with { SequenceNumber = 1UL } }, _changeset, ProjectionOperationType.From, false));
        _released = _converter.ToJsonObject(_results.Single().Event.Content, _own);
    }

    [Fact] void should_have_stored_no_personal_or_confidential_plaintext() => _stored.ToJsonString().ShouldNotContain("historical");
    [Fact] void should_release_the_same_stored_generation_once_through_current_protection() => JsonNode.DeepEquals(_released, _plaintext).ShouldBeTrue();
}
