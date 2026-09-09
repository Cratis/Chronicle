// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.given;

public class a_mutation_validation : Specification
{
    protected EventSequenceKey _scope;
    protected EventSequenceMutationIdentity _targetIdentity;
    protected EventSequenceMutationIdentity _originIdentity;
    protected EventSequenceMutationRequest _request;
    protected EventSequenceMutationTarget _target;
    protected EventSequenceMutationDefinition _definition;
    protected EventSequenceMutation _mutation;
    protected EventSequenceMutationTerminalWitness _witness;
    protected EventSequenceMutationHistoryEntry _history;

    void Establish()
    {
        _scope = new("event-log", "store", "default");
        _targetIdentity = Identity("event-log");
        _originIdentity = Identity("system");
        _request = new(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            _targetIdentity,
            new(_originIdentity, 1UL),
            EventSequenceMutationKind.Revision,
            new("{\"name\":\"Ada\"}", "same-hash"));
        _target = new(10UL, 13UL, 3UL);
        _definition = EventSequenceMutationDefinition.Create(_scope, _request, _target);
        _mutation = new(
            _definition,
            42L,
            7L,
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Unspecified);
        _witness = new(7L, _definition.DefinitionDigestV1, new(new byte[32]));
        _history = new(
            _request.Id,
            42L,
            _request.Origin,
            _request.Kind,
            _request.Command.Hash,
            _target,
            EventSequenceMutationRepairState.NotRequired,
            _witness);
        _history = WithValidReceiptDigest(_history);
        _witness = _history.TerminalWitness;
    }

    protected static EventSequenceMutationIdentity Identity(string display) => EventSequenceMutationIdentity.TryCreate(display).Identity!;

    protected static EventSequenceMutationIdentity IdentityWithKeyFrom(string display, string keyDisplay) =>
        IdentityWithKey(display, new EventSequenceIdentityKey(Encoding.UTF8.GetBytes(keyDisplay)));

    protected static EventSequenceMutationIdentity IdentityWithKey(string display, EventSequenceIdentityKey key)
    {
        var constructor = typeof(EventSequenceMutationIdentity)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 2);
        return (EventSequenceMutationIdentity)constructor.Invoke([display, key]);
    }

    protected EventSequenceMutationDefinition DefinitionFor(
        EventSequenceMutationRequest? request = null,
        EventSequenceMutationTarget? target = null)
    {
        var actualRequest = request ?? _request;
        var actualTarget = target ?? _target;
        return new(
            actualRequest,
            actualTarget,
            EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, actualRequest, actualTarget));
    }

    protected EventSequenceMutationHistoryEntry WithValidReceiptDigest(EventSequenceMutationHistoryEntry history)
    {
        var witness = history.TerminalWitness;
        var digest = EventSequenceMutationDigestCalculator.CalculateReceiptDigest(
            _scope,
            history,
            witness.FinalStateVersion,
            witness.DefinitionDigestV1);
        return history with { TerminalWitness = witness with { ReceiptDigestV1 = digest } };
    }
}
