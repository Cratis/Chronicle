// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_a_constraint_refuses_the_pipeline : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _constraintValidation.Establish(
        Arg.Any<EventSourceId>(),
        Arg.Any<EventTypeId>(),
        Arg.Any<ExpandoObject>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<ConstraintBatchClaims?>(),
        Arg.Any<Subject?>(),
        Arg.Any<IReadOnlyCollection<string>?>())
        .Returns(call => new ConstraintValidationContext([new given.RejectingConstraintValidator()], _eventSourceId, _eventType.Id, call.Arg<ExpandoObject>()));
    async Task Because() => _result = await AppendEffect();

    [Fact] void should_refuse_the_append() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] void should_append_nothing() => _durableStorage.Events.ShouldBeEmpty();
}
