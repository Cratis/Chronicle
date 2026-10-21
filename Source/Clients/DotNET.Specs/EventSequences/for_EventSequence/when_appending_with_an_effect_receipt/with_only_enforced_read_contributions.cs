// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class with_only_enforced_read_contributions : given.a_receipted_append
{
    ReadProtectionCommit _commit;

    void Establish()
    {
        var target = new ReadProtectionTarget("store", "namespace", EventSequenceId.Log);
        _commit = new(target, [new("source", ReadProtectionContributionKind.ProtectedRead, new(7, "source", EventTypes: [new EventType("created", EventTypeGeneration.First)]), target, "decision")], _ => { })
        {
            ValidateReadsWithoutEvents = true
        };
        _response.SequenceNumbers = [];
        _response.EffectReceipt.Outcome = Contracts.Storage.Occurrences.EffectOutcome.NoEvents;
    }

    async Task Because() => _result = await ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt([], _scopes, _receipt, _commit);

    [Fact] void should_send_no_events() => _request.Events.ShouldBeEmpty();
    [Fact] void should_send_the_protected_scope() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(7UL);
    [Fact] void should_send_the_receipt_in_the_same_call() => _request.EffectReceipt.ShouldEqual(_receipt);
}
