// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.when_validating;

public class and_a_batch_reopens_a_persisted_closure : Specification
{
    ConstraintValidationResult _pending;
    ConstraintValidationResult _reviewed;
    ConstraintValidationResult _pendingAfterClosingAgain;

    async Task Because()
    {
        _pending = await Validate(pendingReview: true);
        _reviewed = await Validate(pendingReview: false);
        _pendingAfterClosingAgain = await Validate(pendingReview: true, closeAgain: true);
    }

    [Fact] void should_keep_an_unreviewed_closure_in_effect() => _pending.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_persisted_closing_position() => _pending.Violations[0].SequenceNumber.ShouldEqual((EventSequenceNumber)7);
    [Fact] void should_remove_a_reviewed_closure() => _reviewed.IsValid.ShouldBeTrue();
    [Fact] void should_keep_an_unreviewed_closure_after_another_batch_close_and_reopen() => _pendingAfterClosingAgain.IsValid.ShouldBeFalse();

    static async Task<ConstraintValidationResult> Validate(bool pendingReview, bool closeAgain = false)
    {
        var storage = Substitute.For<IClosedStreamsConstraintStorage>();
        var scope = new ClosedStreamScope(EventSourceId: "source", EventStreamId: "period");
        var definition = new ClosesStreamConstraintDefinition("closing", ["Closed"], scope.Dimensions, ["Reopened"]);
        storage.GetCovering(Arg.Any<ClosedStreamScope>(), Arg.Any<IEnumerable<ClosedStreamDimensions>>())
            .Returns([new ClosedStream(scope, "closing", 7, null, pendingReview)]);
        var claims = new ConstraintBatchClaims();
        ConstraintValidationContext Context(EventTypeId eventType) => new([], "source", eventType, new ExpandoObject(), eventStreamType: "transactions", eventStreamId: "period", batchClaims: claims);
        var observer = (IObserveConstraintBatchEvents)new ClosesStreamConstraintValidator(definition, storage, EventSequenceId.Log);
        observer.RecordBatchEvent(Context("Reopened"));
        if (closeAgain)
        {
            observer.RecordBatchEvent(Context("Closed"));
            observer.RecordBatchEvent(Context("Reopened"));
        }

        return await new ClosedStreamConstraintValidator(storage, [scope.Dimensions], [definition]).Validate(Context("Appended"));
    }
}
