// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_case_variants_are_created_concurrently : given.a_gate
{
    Exception?[] _stores;
    Exception?[] _namespaces;

    void Establish()
    {
        _snapshot.Publish([], []);
        _gate = new(_snapshot, new FakeReservations(), ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
    }

    async Task Because()
    {
        _stores = await Race(Store("Orders"), Store("orders"));
        _namespaces = await Race(Namespace("shop", "Tenant"), Namespace("shop", "tenant"));
    }

    [Fact] void should_let_exactly_one_event_store_win() => _stores.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_refuse_the_other_event_store() => _stores.Single(_ => _ is not null).ShouldBeOfExactType<ResourceNameConflict>();
    [Fact] void should_let_exactly_one_namespace_win() => _namespaces.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_refuse_the_other_namespace() => _namespaces.Single(_ => _ is not null).ShouldBeOfExactType<ResourceNameConflict>();

    async Task<Exception?[]> Race(ResourceCreation first, ResourceCreation second)
    {
        using var barrier = new Barrier(2);
        Task<Exception?> Attempt(ResourceCreation creation) => Task.Run(() =>
        {
            barrier.SignalAndWait();
            return Catch.Exception(() => _gate.EnsureCanCreate(creation));
        });

        return await Task.WhenAll(Attempt(first), Attempt(second));
    }
}
