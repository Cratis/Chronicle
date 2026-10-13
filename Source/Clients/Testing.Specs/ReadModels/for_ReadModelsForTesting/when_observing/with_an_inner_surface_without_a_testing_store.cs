// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_an_inner_surface_without_a_testing_store : given.a_collection
{
    Exception _exception;

    void Because()
    {
        var readModels = new ReadModelsForTesting(Substitute.For<IReadModels>(), _store.EventLog);
        _exception = Catch.Exception(() => readModels.Observe<ObservedModule>());
    }

    [Fact] void should_refuse_observation_without_the_owning_store() => _exception.ShouldBeOfExactType<ReadModelCannotBeObserved>();
}
