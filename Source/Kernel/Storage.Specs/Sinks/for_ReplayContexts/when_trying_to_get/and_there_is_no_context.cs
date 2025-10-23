// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sinks.for_ReplayContexts.when_trying_to_get;

public class and_there_is_no_context : Specification
{
    static ReadModelIdentifier _readModelIdentifier = "SomeModelId";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    Result<ReplayContext, GetContextError> _result;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _storage.TryGet(_readModelIdentifier).Returns(GetContextError.NotFound);
        _contexts = new(_storage);
    }

    async Task Because() => _result = await _contexts.TryGet(_readModelIdentifier);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
}
