// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts;

/// <summary>
/// An evicted context must be gone from the cache as well as from storage - a cached one outlived its replay,
/// and the next replay's end used its revert name, which already existed, so the swap failed (#4296).
/// </summary>
public class when_evicting_an_established_context : Specification
{
    readonly ReadModelType _type = new("SomeModelId", ReadModelGeneration.First);
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    Result<ReplayContext, GetContextError> _result;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _storage.TryGet(_type.Identifier).Returns(GetContextError.NotFound);
        _contexts = new(_storage);
    }

    async Task Because()
    {
        await _contexts.Establish(_type, "some-container");
        await _contexts.Evict(_type.Identifier);
        _result = await _contexts.TryGet(_type.Identifier);
    }

    [Fact] void should_no_longer_hold_the_context() => _result.IsSuccess.ShouldBeFalse();
}
