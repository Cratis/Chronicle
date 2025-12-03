// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks.for_ReplayContexts;

public class when_establishing : Specification
{
    ReadModelIdentifier _readModelId = "SomeModelId";
    ReadModelName _readModelName = "SomeModel";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    ReplayContext _context;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _contexts = new(_storage);
    }

    async Task Because() => _context = await _contexts.Establish(_readModelId, _readModelName);

    [Fact] void should_return_context() => _context.ShouldNotBeNull();
    [Fact] void should_have_model_in_context() => _context.ReadModelIdentifier.ShouldEqual(_readModelId);
    [Fact] void should_save_context() => _storage.Received(1).Save(_context);
}
