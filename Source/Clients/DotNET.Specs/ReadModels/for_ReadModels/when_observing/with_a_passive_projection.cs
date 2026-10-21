// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_a_passive_projection : given.a_collection_stream
{
    Exception _failure;

    void Establish() => ((IKnowPassiveProjections)_projections).IsPassive(typeof(Row)).Returns(true);
    void Because() => _failure = Catch.Exception(() => _readModels.Observe<Row>());

    [Fact] void should_refuse_observation() => _failure.ShouldBeOfExactType<ReadModelCannotBeObserved>();
    [Fact] void should_not_open_a_stream() => _service.ReceivedCalls().ShouldBeEmpty();
}
