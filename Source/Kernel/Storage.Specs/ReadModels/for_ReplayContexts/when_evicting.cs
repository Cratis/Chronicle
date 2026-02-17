// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts;

public class when_evicting : Specification
{
    ReadModelIdentifier _readModelId = "SomeModelId";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _contexts = new(_storage);
    }

    Task Because() => _contexts.Evict(_readModelId);

    [Fact] void should_remove_from_storage() => _storage.Received(1).Remove(_readModelId);
}
