// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_effect_receipt : given.a_receipted_unit
{
    void Establish()
    {
        _unitOfWork.UseEffectReceipt(_key, JsonNode.Parse("{\"accepted\":true}"));
        _unitOfWork.AddEventWithNamedTags(EventSequenceId.Log, "source", "created", [new("category", "receipt")], Causation.Unknown());
        _receiptResult = _receiptResult with { Receipt = _receiptResult.Receipt with { Outcome = new Appended(EventSequenceId.Log, 4, 4) } };
    }

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_commit_once_with_the_receipt() => _receiptedAppends.ShouldEqual(1);
    [Fact] void should_carry_the_effect_name() => _sentReceipt.EffectKey.ShouldEqual(_key.Name.Value);
    [Fact] void should_carry_the_response() => _sentReceipt.Response.ShouldEqual("{\"accepted\":true}");
    [Fact] void should_preserve_named_tags() => _eventsAppended.Single().NamedTags.Single().Value.ShouldEqual("receipt");
    [Fact] void should_expose_the_winning_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);
    [Fact] void should_not_fall_back_to_an_unreceipted_append() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_not_record_separately() => _occurrences.DidNotReceive().RecordEffectReceipt(Arg.Any<EffectKey>(), Arg.Any<EffectOutcome>(), Arg.Any<JsonNode>());
}
