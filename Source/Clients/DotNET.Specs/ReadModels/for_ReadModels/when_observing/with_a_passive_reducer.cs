// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_a_passive_reducer : for_ReadModels.given.all_dependencies
{
    Exception _failure;

    void Establish() => _reducers.HasFor(Arg.Is<Type>(type => type == typeof(PassiveRow))).Returns(true);
    void Because() => _failure = Catch.Exception(() => _readModels.Observe<PassiveRow>());

    [Fact] void should_refuse_observation() => _failure.ShouldBeOfExactType<ReadModelCannotBeObserved>();

    [Passive]
    public record PassiveRow(string Name);
}
