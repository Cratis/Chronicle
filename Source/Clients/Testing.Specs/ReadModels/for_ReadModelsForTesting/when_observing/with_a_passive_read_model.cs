// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_passive_read_model : given.a_collection
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() => _store.ReadModels.Observe<SimpleModule>());

    [Fact] void should_refuse_the_same_shape_as_the_client() => _failure.ShouldBeOfExactType<ReadModelCannotBeObserved>();
}
