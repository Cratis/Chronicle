// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_is_registered_after_activation;

public class and_appending_an_event_that_violates_it : given.an_event_sequence
{
    IConstraintValidation _rejectingValidation;
    AppendResult _result;

    void Establish()
    {
        var rejectingValidator = new given.RejectingConstraintValidator();
        _rejectingValidation = Substitute.For<IConstraintValidation>();
        _rejectingValidation.Establish(
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

        // The grain already activated (in the base Establish) with no constraints; register one now.
        _registeredConstraints.Add(new UniqueConstraintDefinition(
            "unique-thing",
            [new UniqueConstraintEventDefinition(_eventType.Id, ["Some"])]));
        _currentValidation = _rejectingValidation;
    }

    async Task Because() => _result = await _eventSequence.Append(
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

    [Fact] void should_reject_the_append() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_start_a_reindex_job() =>
        _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
