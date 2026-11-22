// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces.for_NamespaceLifecycleStorage;

public class when_competing_to_initialize_a_name : Specification
{
    NamespaceLifecycleStorage _storage;
    NamespaceLifecycle _first;
    NamespaceLifecycle _second;
    NamespaceLifecycle _result;
    bool _firstWon;
    bool _secondWon;

    void Establish()
    {
        _storage = new();
        _first = new("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch);
        _second = new("ORDERS", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch);
    }

    async Task Because()
    {
        _firstWon = await _storage.TryWrite("orders", null, _first);
        _secondWon = await _storage.TryWrite("ORDERS", null, _second);
        _result = await _storage.Get("OrDeRs");
    }

    [Fact] void should_accept_one_prepared_identity() => _firstWon.ShouldBeTrue();
    [Fact] void should_reject_a_second_identity_for_the_same_name() => _secondWon.ShouldBeFalse();
    [Fact] void should_preserve_the_winning_canonical_name() => _result.ShouldEqual(_first);
}
