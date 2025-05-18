// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.Sinks.for_ReplayContexts.when_trying_to_get;

public class and_there_is_a_context : Specification
{
    static ModelName _model = "SomeModel";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    ReplayContext _context;
    Result<ReplayContext, GetContextError> _result;


    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _context = new(_model, $"{_model}-20210901120000", DateTimeOffset.UtcNow);
        _storage.TryGet(_model).Returns(_context);
        _contexts = new(_storage);
    }

    async Task Because() => _result = await _contexts.TryGet(_model);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_context() => _result.AsT0.ShouldEqual(_context);
}
