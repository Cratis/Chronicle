// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_observing_through_its_read_models : a_borrowed_store
{
    Exception _borrowedError;
    Exception _freshError;

    void Because()
    {
        _borrowedError = Catch.Exception(() => Borrowed<Tally>().ReadModels.Observe<Tally>());
        _freshError = Catch.Exception(() => Fresh<Tally>().ReadModels.Observe<Tally>());
    }

    [Fact] void should_refuse_observation() => _borrowedError.ShouldBeOfExactType<ReadModelCannotBeObserved>();
    [Fact] void should_refuse_it_like_a_fresh_scenario() => _freshError.ShouldBeOfExactType<ReadModelCannotBeObserved>();
}
