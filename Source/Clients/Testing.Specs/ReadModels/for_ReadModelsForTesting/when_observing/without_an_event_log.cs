// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class without_an_event_log : Specification
{
    ReadModelsForTesting _readModels;
    Exception _error;

    void Establish() => _readModels = new(Substitute.For<IReadModels>());
    void Because() => _error = Catch.Exception(() => _readModels.Observe<SimpleModule>());

    [Fact] void should_refuse_observation() => _error.ShouldBeOfExactType<ReadModelCannotBeObserved>();
}
