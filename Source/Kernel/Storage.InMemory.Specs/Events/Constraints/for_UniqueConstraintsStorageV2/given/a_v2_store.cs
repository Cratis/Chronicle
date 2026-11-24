// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public class a_v2_store : Specification
{
    protected UniqueConstraintsStorageV2 _storage;
    protected UniqueConstraintDefinition _definition;
    protected UniqueConstraintDefinition _perValue;
    protected UniqueConstraintWriteAuthority _authority;
    protected UniqueConstraintStorageScope _scope;
    protected UniqueConstraintStorageScope _otherScope;
    protected UniqueConstraintValue _value;
    protected UniqueConstraintValue _otherValue;
    protected EventSourceId _source;
    protected EventSourceId _otherSource;
    protected Subject _subject;

    void Establish()
    {
        _storage = new();
        _definition = new("constraint", []);
        _perValue = _definition with { Mode = UniqueConstraintMode.PerValue };
        _authority = new(new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), UniqueConstraintWritePhase.Live, new(0));
        _scope = new(new string('0', 32));
        _otherScope = new(new string('1', 32));
        _value = new(new string('a', 64));
        _otherValue = new(new string('b', 64));
        _source = "source";
        _otherSource = "other";
        _subject = "subject";
    }

    protected async Task Seed()
    {
        (await _storage.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_source, _definition, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
    }

    protected Task<Result<bool, UniqueConstraintWriteFailure>>[] Mutations(UniqueConstraintWriteAuthority proof) =>
        Enumerable.Range(0, 6).Select(_ => Mutation(_, proof)).ToArray();

    protected Task<Result<bool, UniqueConstraintWriteFailure>> Mutation(int operation, UniqueConstraintWriteAuthority proof, CancellationToken cancellationToken = default) => operation switch
    {
        0 => _storage.Save(_source, _definition, 20, _otherValue, _scope, proof, cancellationToken: cancellationToken),
        1 => _storage.Remove(_source, _definition, _scope, proof, cancellationToken),
        2 => _storage.RemoveValue(_source, _definition, _value, _scope, proof, cancellationToken),
        3 => _storage.ClearValues(_definition, _scope, proof, cancellationToken),
        4 => _storage.RemoveAllFor(_definition, proof, cancellationToken),
        5 => _storage.RemoveForSubject(_definition, _subject, proof, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    protected static UniqueConstraintWritePhase Run(int suffix) => UniqueConstraintWritePhase.Run(new(new Guid(suffix, 0, 0, new byte[8])));
}
