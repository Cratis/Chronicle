// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels.for_ReplayContexts;

public class when_establishing_twice_in_the_same_second : Specification
{
    ReadModelType _readModelType = new("SomeModelId", ReadModelGeneration.First);
    ReadModelContainerName _readModelName = "SomeModel";
    ReplayContexts _contexts;
    ReplayContext _first;
    ReplayContext _second;

    void Establish() => _contexts = new(Substitute.For<IReplayContextsStorage>());

    async Task Because()
    {
        _first = await _contexts.Establish(_readModelType, _readModelName);
        _second = await _contexts.Establish(_readModelType, _readModelName);
    }

    [Fact] void should_name_a_different_revert_container_for_each() => _second.RevertContainerName.ShouldNotEqual(_first.RevertContainerName);
    [Fact] void should_keep_the_read_model_name_as_the_revert_prefix() => _second.RevertContainerName.Value.StartsWith($"{_readModelName}-", StringComparison.Ordinal).ShouldBeTrue();
}
