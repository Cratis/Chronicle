// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts.when_trying_to_get;

public class and_there_is_a_context : Specification
{
    ReadModelType _readModelType = new("SomeModelId", ReadModelGeneration.First);
    ReadModelName _readModel = "SomeModel";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    ReplayContext _context;
    Result<ReplayContext, GetContextError> _result;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _context = new(_readModelType, _readModel, $"{_readModel}-20210901120000", DateTimeOffset.UtcNow);
        _storage.TryGet(_readModelType.Identifier).Returns(_context);
        _contexts = new(_storage);
    }

    async Task Because() => _result = await _contexts.TryGet(_readModelType.Identifier);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_context() => _result.AsT0.ShouldEqual(_context);
}
