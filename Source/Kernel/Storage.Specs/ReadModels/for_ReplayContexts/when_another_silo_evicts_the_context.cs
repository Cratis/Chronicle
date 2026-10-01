// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts;

public class when_another_silo_evicts_the_context : Specification
{
    ReplayContexts _first;
    ReplayContexts _second;
    IReplayContextsStorage _storage;
    readonly ReadModelType _type = new("model", ReadModelGeneration.First);
    Result<ReplayContext, GetContextError> _result;
    async Task Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _first = new(_storage);
        _second = new(_storage);
        await _first.Establish(_type, "Model");
        _storage.TryGet(_type.Identifier).Returns(GetContextError.NotFound);
    }
    async Task Because()
    {
        await _second.Evict(_type.Identifier);
        _result = await _first.TryGet(_type.Identifier);
    }
    [Fact] void should_not_reuse_a_silo_local_stale_context() => _result.IsSuccess.ShouldBeFalse();
}
