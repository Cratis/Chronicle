// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reusing_an_operation_with_changed_inputs<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    Exception _error;
    async Task Establish() => await Build();
    async Task Because() => _error = await Catch.Exception(async () => await _builds.ReserveNext(_store, _key, MaterializationExecutionSpecification.Capture(_execution.EventSequence, "{\"changed\":true}"), _operation, DateTimeOffset.UnixEpoch));
    [Fact] public void should_refuse_the_conflicting_retry() => _error.ShouldBeOfExactType<GenerationBuildConflict>();
}
