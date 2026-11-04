// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page;

public class and_the_reactor_cannot_be_activated : given.a_reactor_dispatcher
{
    void Establish() => _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(NotifiedReactor))
        .Returns((Cratis.Monads.Catch<ActivatedArtifact>)new Exception("Cannot activate"));

    async Task Because() => await _dispatcher.Dispatch(_handler, null, PageWith(("first", [1]), ("second", [2])), "ns", Publish, CancellationToken.None);

    [Fact] void should_fail_every_partition() => _published[0].Partitions.Select(_ => _.State).ShouldContainOnly(ObservationState.Failed, ObservationState.Failed);
    [Fact] void should_carry_the_activation_message() => _published[0].Partitions.All(_ => _.ExceptionMessages.Contains("Cannot activate")).ShouldBeTrue();
    [Fact] void should_not_advance_any_partition() => _published[0].Partitions.All(_ => _.LastSuccessfulObservation == ulong.MaxValue).ShouldBeTrue();
}
