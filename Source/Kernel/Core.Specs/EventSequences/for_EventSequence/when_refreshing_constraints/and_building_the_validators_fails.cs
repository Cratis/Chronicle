// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_refreshing_constraints;

/// <summary>
/// A refresh that could not build the validators must not mark the sequence as current: it keeps its previous
/// version, so the next version check sees the change and retries instead of trusting the stale validators.
/// </summary>
public class and_building_the_validators_fails : given.an_event_sequence
{
    Exception _refreshError;
    AppendResult _result;

    void Establish()
    {
        _registeredConstraints.Add(new UniqueConstraintDefinition(
            "unique-thing",
            [new UniqueConstraintEventDefinition(_eventType.Id, ["Some"])]));
        _constraintValidationFactory.Create(Arg.Any<EventSequenceKey>()).Returns(Task.FromException<IConstraintValidation>(new InvalidOperationException()));
    }

    async Task Because()
    {
        _refreshError = await Catch.Exception(_eventSequence.RefreshConstraints);

        var rejectingValidator = new given.RejectingConstraintValidator();
        var rejectingValidation = Substitute.For<IConstraintValidation>();
        rejectingValidation.Establish(
            Arg.Any<EventSourceId>(),
            Arg.Any<EventTypeId>(),
            Arg.Any<ExpandoObject>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<ConstraintBatchClaims?>())
            .Returns(callInfo => new ConstraintValidationContext(
                [rejectingValidator],
                callInfo.ArgAt<EventSourceId>(0),
                callInfo.ArgAt<EventTypeId>(1),
                callInfo.ArgAt<ExpandoObject>(2)));
        _constraintValidationFactory.Create(Arg.Any<EventSequenceKey>()).Returns(rejectingValidation);

        _result = await _eventSequence.Append(
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            new JsonObject(),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            ConcurrencyScope.None);
    }

    [Fact] void should_fail_the_refresh() => _refreshError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_on_the_next_append_and_validate_against_the_current_constraints() => _result.HasConstraintViolations.ShouldBeTrue();
}
