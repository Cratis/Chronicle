// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry.given;

public class a_registry_contract : Specification
{
    protected EventSequenceKey _scope;
    protected EventSequenceMutationIdentity _identity;
    protected EventSequenceMutationRequest _request;
    protected EventSequenceMutationTarget _target;
    protected EventSequenceMutationDefinition _definition;
    protected EventSequenceMutation _active;
    protected EventSequenceMutationStateToken _token;
    protected EventSequenceMutationHistoryEntry _history;
    protected EventSequenceMutationRegistration _archivedRegistration;
    protected EventSequenceMutationRegistration _activeRegistration;
    protected EventSequenceMutationValidationResult _invalidValidation;

    void Establish()
    {
        _scope = new("target-sequence", "event-store", "namespace");
        _identity = EventSequenceMutationIdentity.TryCreate("target-sequence").Identity!;
        var origin = EventSequenceMutationIdentity.TryCreate("origin-sequence").Identity!;
        _request = new(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            _identity,
            new(origin, 42UL),
            EventSequenceMutationKind.Revision,
            new("{\"privateCommand\":\"must-not-leak\"}", "command-hash"));
        _target = new(10UL, 13UL, 3UL);
        _definition = EventSequenceMutationDefinition.Create(_scope, _request, _target);
        _active = new(
            _definition,
            7L,
            1L,
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Unspecified);
        _token = EventSequenceMutationStateToken.Create(_scope, _active);
        var witness = new EventSequenceMutationTerminalWitness(1L, _definition.DefinitionDigestV1, new(new byte[32]));
        _history = new(
            _request.Id,
            7L,
            _request.Origin,
            _request.Kind,
            _request.Command.Hash,
            _target,
            EventSequenceMutationRepairState.NotRequired,
            witness);
        var receiptDigest = EventSequenceMutationDigestCalculator.CalculateReceiptDigest(_scope, _history, witness.FinalStateVersion, witness.DefinitionDigestV1);
        _history = _history with { TerminalWitness = witness with { ReceiptDigestV1 = receiptDigest } };
        _archivedRegistration = new(_definition, EventSequenceMutationRegistryLifecycle.Archived, 7L, _history.TerminalWitness);
        _activeRegistration = new(_definition, EventSequenceMutationRegistryLifecycle.Bound, 7L, null);
        _invalidValidation = EventSequenceMutationValidator.ValidateRequest(null);
    }
}
