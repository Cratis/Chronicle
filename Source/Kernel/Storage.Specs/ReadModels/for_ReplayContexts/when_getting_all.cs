// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts;

public class when_getting_all : Specification
{
    IReplayContextsStorage _storage;
    ReplayContexts _contexts;
    IReadOnlyList<ReplayContext> _actual;
    readonly IReadOnlyList<ReplayContext> _expected = [new(new("orders", ReadModelGeneration.First), "orders", "orders-revert", DateTimeOffset.FromUnixTimeSeconds(10000))];

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _storage.GetAll().Returns(_expected);
        _contexts = new(_storage);
    }

    async Task Because() => _actual = await _contexts.GetAll();

    [Fact] void should_return_durable_sessions() => _actual.ShouldEqual(_expected);
    [Fact] void should_read_once() => _storage.Received(1).GetAll();
    [Fact] void should_not_delete() => _storage.DidNotReceiveWithAnyArgs().Remove(Arg.Any<ReadModelIdentifier>());
    [Fact] void should_not_save() => _storage.DidNotReceiveWithAnyArgs().Save(Arg.Any<ReplayContext>());
}
