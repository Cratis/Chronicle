// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_ReplayContexts;

public class when_getting_all : Specification
{
    readonly ReadModels.ReplayContexts _contexts = new();
    ReplayContext[] _expected;
    IReadOnlyList<ReplayContext> _actual;

    async Task Establish() => _expected = [await _contexts.Establish(new("orders", ReadModelGeneration.First), "orders"), await _contexts.Establish(new("items", ReadModelGeneration.First), "items")];

    async Task Because() => _actual = await _contexts.GetAll();

    [Fact] void should_read_both_sessions() => _actual.OrderBy(context => context.Type.Identifier.Value).ShouldEqual(_expected.OrderBy(context => context.Type.Identifier.Value));
    [Fact] async Task should_not_remove_sessions() => (await _contexts.GetAll()).Count.ShouldEqual(2);
}
