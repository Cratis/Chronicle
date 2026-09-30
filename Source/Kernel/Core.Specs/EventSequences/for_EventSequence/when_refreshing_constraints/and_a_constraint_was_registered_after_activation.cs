// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_refreshing_constraints;

/// <summary>
/// A reindex of this sequence is about to start, so the sequence must validate and index against the current
/// constraints from now on - not only once its throttled version check next comes due.
/// </summary>
public class and_a_constraint_was_registered_after_activation : given.an_event_sequence
{
    AppendResult _result;

    protected override TimeSpan ConstraintsVersionCheckInterval => TimeSpan.FromHours(1);

    async Task Establish()
    {
        // Spend the check that is always due on the first append, so only the refresh can pick the constraint up.
        await Append();

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

        _registeredConstraints.Add(new UniqueConstraintDefinition(
            "unique-thing",
            [new UniqueConstraintEventDefinition(_eventType.Id, ["Some"])]));
        _currentValidation = rejectingValidation;
    }

    async Task Because()
    {
        await _eventSequence.RefreshConstraints();
        _result = await Append();
    }

    [Fact] void should_validate_the_next_append_against_the_refreshed_constraints() => _result.HasConstraintViolations.ShouldBeTrue();

    Task<AppendResult> Append() => _eventSequence.Append(
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
