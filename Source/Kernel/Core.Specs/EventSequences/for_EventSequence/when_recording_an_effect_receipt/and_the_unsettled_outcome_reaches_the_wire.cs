// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_unsettled_outcome_reaches_the_wire : given.an_event_sequence_with_effect_receipts
{
    Services.Occurrences.Occurrences _service;
    Contracts.Commands.CommandResult<Contracts.Occurrences.RecordEffectReceiptResponse> _wireResult;

    void Establish()
    {
        _collision = EffectReceiptWriteResult.TransactionConflicted;
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute<RecordEffectReceiptResult>(Arg.Any<object>(), Arg.Any<ValidationResultSeverity?>()).Returns(async call =>
        {
            var command = (RecordEffectReceipt)call.Arg<object>();
            var error = await Catch.Exception(() => _eventSequence.RecordEffectReceipt(command.EffectReceipt, command.Outcome, command.Reasons.ToArray()));

            // Arc captures handler failures in the command result. Exercise that boundary through the generated service.
            return new CommandResult<RecordEffectReceiptResult>(CorrelationId.New(), null)
            {
                ExceptionMessages = [error.Message]
            };
        });
        _service = new(pipeline, _storage, NullLogger<Services.Occurrences.Occurrences>.Instance);
    }

    async Task Because() => _wireResult = await _service.RecordEffectReceipt(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EffectReceipt = new() { OccurrenceId = _receipt.OccurrenceId, EffectKey = _receipt.EffectKey, Epoch = _receipt.Epoch, Response = _receipt.Response },
        Outcome = Contracts.Storage.Occurrences.EffectOutcome.NoEvents,
        Reasons = []
    });

    [Fact] void should_not_report_success() => _wireResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_retryable_failure_name() => _wireResult.ExceptionMessages.ShouldContainOnly("EffectReceiptUnsettled");
    [Fact] void should_not_report_a_business_refusal() => _wireResult.ValidationResults.ShouldBeEmpty();
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
}
