// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_an_event_target : for_ReadModels.given.all_dependencies
{
    Exception _failure;

    void Establish() => _projections.HasFor(Arg.Is<Type>(type => type == typeof(Published))).Returns(true);
    void Because() => _failure = Catch.Exception(() => _readModels.Observe<Published>());

    [Fact] void should_refuse_observation() => _failure.ShouldBeOfExactType<ReadModelCannotBeObserved>();

    [EventType]
    public record Published(string Name);
}
