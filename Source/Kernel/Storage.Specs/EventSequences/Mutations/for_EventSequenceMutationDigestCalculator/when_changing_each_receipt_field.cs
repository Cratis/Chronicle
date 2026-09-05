// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_changing_each_receipt_field : given.a_digest_calculation
{
    EventSequenceMutationReceiptDigestV1 _baseline;
    EventSequenceMutationReceiptDigestV1[] _changed;

    void Because()
    {
        _baseline = CalculateReceipt();
        _changed =
        [
            CalculateReceipt(scope: _scope with { EventStore = "another-store" }),
            CalculateReceipt(scope: _scope with { Namespace = "another-namespace" }),
            CalculateReceipt(scope: _scope with { EventSequenceId = "another-target" }),
            CalculateReceipt(receipt: _receipt with { Id = Guid.Parse("10112233-4455-6677-8899-aabbccddeeff") }),
            CalculateReceipt(receipt: _receipt with { Ordinal = 43L }),
            CalculateReceipt(receipt: _receipt with { Origin = _receipt.Origin with { Sequence = EventSequenceMutationIdentity.TryCreate("another-origin").Identity! } }),
            CalculateReceipt(receipt: _receipt with { Origin = _receipt.Origin with { SequenceNumber = 2UL } }),
            CalculateReceipt(receipt: _receipt with { Kind = EventSequenceMutationKind.PointRedaction }),
            CalculateReceipt(receipt: _receipt with { CommandHash = "another-hash" }),
            CalculateReceipt(receipt: _receipt with { Target = _receipt.Target with { Start = 9UL } }),
            CalculateReceipt(receipt: _receipt with { Target = _receipt.Target with { EndExclusive = 14UL } }),
            CalculateReceipt(receipt: _receipt with { Target = _receipt.Target with { ExpectedCount = 4UL } }),
            CalculateReceipt(receipt: _receipt with { RepairState = EventSequenceMutationRepairState.Unknown }),
            CalculateReceipt(finalStateVersion: 8L),
            CalculateReceipt(definitionDigest: new EventSequenceMutationDefinitionDigestV1(new byte[32]))
        ];
    }

    [Fact] void should_make_every_framed_field_affect_the_digest() => _changed.All(_ => _ != _baseline).ShouldBeTrue();
    [Fact] void should_cover_every_receipt_field() => _changed.Length.ShouldEqual(15);
}
