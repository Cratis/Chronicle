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
        var request = new EventSequenceMutationRequest(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            EventSequenceMutationIdentity.TryCreate("event-log").Identity!,
            new(EventSequenceMutationIdentity.TryCreate("system").Identity!, 1UL),
            EventSequenceMutationKind.Revision,
            new("{\"name\":\"Ada\"}", "same-hash"));
        var target = new EventSequenceMutationTarget(10UL, 13UL, 3UL);
        var definition = EventSequenceMutationDefinition.Create(_scope, request, target);
        _mutation = new(
            definition,
            42L,
            1L,
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Accepted);
        _finalStateVersion = 7L;
        _definitionDigest = definition.DefinitionDigestV1;
        _receipt = new(
            _mutation.Id,
            _mutation.Ordinal,
            _mutation.Origin,
            _mutation.Kind,
            _mutation.Command.Hash,
            _mutation.Target,
            _mutation.RepairState,
            new(_finalStateVersion, _definitionDigest, new(new byte[32])));
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
