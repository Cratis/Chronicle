// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_the_reactor_cannot_be_activated : given.a_reactor_dispatcher
{
    void Establish() => _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(NotifiedReactor))
        .Returns((Cratis.Monads.Catch<ActivatedArtifact>)new Exception("Cannot activate"));

    async Task Because() => await _dispatcher.Dispatch(_handler, null, EventsWith(1), "ns", Publish, CancellationToken.None);

    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_fail() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_carry_the_activation_message() => _published[0].ExceptionMessages.ShouldContain("Cannot activate");
    [Fact] void should_not_advance_the_checkpoint() => _published[0].LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
}
