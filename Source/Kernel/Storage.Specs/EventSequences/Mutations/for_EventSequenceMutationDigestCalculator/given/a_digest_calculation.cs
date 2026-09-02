// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator.given;

public class a_digest_calculation : Specification
{
    protected EventSequenceKey _scope;
    protected EventSequenceMutation _mutation;
    protected EventSequenceMutationHistoryEntry _receipt;
    protected EventSequenceMutationStateVersion _finalStateVersion;
    protected EventSequenceMutationDefinitionDigestV1 _definitionDigest;

    void Establish()
    {
        _scope = new("event-log", "store", "default");
        _mutation = new(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            42L,
            new("system", 1UL),
            new(EventSequenceMutationKind.Revision, "{\"name\":\"Ada\"}", "same-hash"),
            new(10UL, 13UL, 3UL),
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Accepted);
        _receipt = new(
            _mutation.Id,
            _mutation.Ordinal,
            _mutation.Origin,
            _mutation.Command.Kind,
            _mutation.Command.Hash,
            _mutation.Target,
            _mutation.RepairState);
        _finalStateVersion = 7L;
        _definitionDigest = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _mutation);
    }

    protected EventSequenceMutationDefinitionDigestV1 CalculateDefinition(EventSequenceKey? scope = null, EventSequenceMutation? mutation = null) =>
        EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(scope ?? _scope, mutation ?? _mutation);

    protected EventSequenceMutationReceiptDigestV1 CalculateReceipt(
        EventSequenceKey? scope = null,
        EventSequenceMutationHistoryEntry? receipt = null,
        EventSequenceMutationStateVersion? finalStateVersion = null,
        EventSequenceMutationDefinitionDigestV1? definitionDigest = null) =>
        EventSequenceMutationDigestCalculator.CalculateReceiptDigest(
            scope ?? _scope,
            receipt ?? _receipt,
            finalStateVersion ?? _finalStateVersion,
            definitionDigest ?? _definitionDigest);
}
