// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintsProvider;

/// <summary>
/// Two event types declaring <c language="csharp">[Unique]</c> under one name are one constraint, as they are when declared with
/// <c language="csharp">Unique&lt;A&gt;(name: x)</c> and <c language="csharp">Unique&lt;B&gt;(name: x)</c> on a builder. The provider used to return a definition
/// for each, so neither checked the other's event type and the name resolved to two definitions.
/// </summary>
public class when_providing_same_named_declarations : Specification
{
    const string SharedName = "OneClaimPerSource";
    const string FirstMessage = "The first message";
    const string SecondMessage = "The second message";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    EventType _firstEventType;
    EventType _secondEventType;
    EventType _unrelatedEventType;
    EventType _firstRemovalEventType;
    EventType _secondRemovalEventType;
    IImmutableList<IConstraintDefinition> _result;
    UniqueEventTypeConstraintDefinition _merged;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _firstEventType = new EventType(nameof(FirstClaim), EventTypeGeneration.First);
        _secondEventType = new EventType(nameof(SecondClaim), EventTypeGeneration.First);
        _unrelatedEventType = new EventType(nameof(UnrelatedClaim), EventTypeGeneration.First);
        _firstRemovalEventType = new EventType(nameof(FirstRelease), EventTypeGeneration.First);
        _secondRemovalEventType = new EventType(nameof(SecondRelease), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(FirstClaim)).Returns(_firstEventType);
        _eventTypes.GetEventTypeFor(typeof(SecondClaim)).Returns(_secondEventType);
        _eventTypes.GetEventTypeFor(typeof(UnrelatedClaim)).Returns(_unrelatedEventType);
        _eventTypes.GetEventTypeFor(typeof(FirstRelease)).Returns(_firstRemovalEventType);
        _eventTypes.GetEventTypeFor(typeof(SecondRelease)).Returns(_secondRemovalEventType);

        _clientArtifactsProvider.UniqueEventTypeConstraints.Returns([typeof(FirstClaim), typeof(UnrelatedClaim), typeof(SecondClaim)]);
        _clientArtifactsProvider.RemoveConstraintEventTypes.Returns([typeof(FirstRelease), typeof(SecondRelease)]);
    }

    void Because()
    {
        _result = new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes).Provide();
        _merged = (UniqueEventTypeConstraintDefinition)_result.Single(_ => _.Name == (ConstraintName)SharedName);
    }

    [Fact] void should_return_one_definition_for_the_shared_name_and_one_for_the_other() => _result.Count.ShouldEqual(2);
    [Fact] void should_keep_the_unrelated_constraint_separate() => _result.Count(_ => _.Name == (ConstraintName)nameof(UnrelatedClaim)).ShouldEqual(1);
    [Fact] void should_check_every_declaring_event_type() => _merged.EventTypeIds.ShouldContainOnly([_firstEventType.Id, _secondEventType.Id]);
    [Fact] void should_release_on_every_removal_event() => _merged.RemovedWith.ShouldContainOnly([_firstRemovalEventType.Id, _secondRemovalEventType.Id]);
    [Fact] void should_apply_to_every_event_sequence_when_one_declaration_names_none() => _merged.EventSequences.ShouldBeEmpty();
    [Fact] void should_use_the_message_of_the_first_event_type_when_it_violates() => _merged.MessageCallback(ViolationFor(_firstEventType)).ShouldEqual((ConstraintViolationMessage)FirstMessage);
    [Fact] void should_use_the_message_of_the_second_event_type_when_it_violates() => _merged.MessageCallback(ViolationFor(_secondEventType)).ShouldEqual((ConstraintViolationMessage)SecondMessage);

    static ConstraintViolation ViolationFor(EventType eventType) =>
        new(eventType.Id, EventSequenceNumber.First, ConstraintType.UniqueEventType, SharedName, string.Empty, new());

    [Unique(SharedName, FirstMessage, EventSequences = [EventSequenceId.LogId])] record FirstClaim;
    [Unique(SharedName, SecondMessage)] record SecondClaim;
    [Unique] record UnrelatedClaim;
    [RemoveConstraint(SharedName)] record FirstRelease;
    [RemoveConstraint(SharedName)] record SecondRelease;
}
