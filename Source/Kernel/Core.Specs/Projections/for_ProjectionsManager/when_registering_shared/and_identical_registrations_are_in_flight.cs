// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

/// <summary>
/// After a kernel restart every replica of a client, and each of its retries, registers the same set. Queueing each
/// behind the last made the queue outlast the callers' timeout. An identical registration that arrives while one is
/// in flight waits for it instead, and a different one is not mistaken for it.
/// </summary>
public class and_identical_registrations_are_in_flight : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _firstFinishes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _queuedRegistrations;
    Task _leader;
    Task _joiner;
    Task _anotherJoiner;
    Task _different;
    Task _differentOwner;
    Task _differentGenerationDelivery;
    bool _equalRequestsPendingBeforeRelease;
    int _registrationsQueuedBeforeRelease;

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _queuedRegistrations
            .Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<Concepts.Projections.ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(_ => _firstFinishes.Task);
        _silo.AddProbe(_ => _queuedRegistrations);
    }

    async Task Because()
    {
        static ProjectionDefinition[] Definitions() => [CreateDefinition("the-projection", "the-read-model")];

        Task Register(ProjectionDefinition[] definitions, ProjectionOwner? fullSetOwner = null) =>
            _grain.RegisterShared(ProjectionRegistrationSignature.For(definitions, fullSetOwner, "the-app", null), definitions, fullSetOwner, "the-app");

        _leader = Register(Definitions());
        _joiner = Register(Definitions());
        _anotherJoiner = Register(Definitions());
        _different = Register([CreateDefinition("the-projection", "another-read-model")]);
        _differentOwner = Register(Definitions(), ProjectionOwner.Client);
        _differentGenerationDelivery = Register([Definitions()[0] with { GenerationDelivery = EventGenerationDelivery.Pinned }]);
        _equalRequestsPendingBeforeRelease = !_leader.IsCompleted && !_joiner.IsCompleted && !_anotherJoiner.IsCompleted;
        _registrationsQueuedBeforeRelease = _queuedRegistrations.ReceivedCalls().Count();
        _firstFinishes.SetResult();
        await Task.WhenAll(_leader, _joiner, _anotherJoiner, _different, _differentOwner, _differentGenerationDelivery);
    }

    [Fact] void should_queue_the_work_once_for_identical_registrations() => _queuedRegistrations.Received(1).Register(Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Single().ReadModel == "the-read-model" && definitions.Single().GenerationDelivery != EventGenerationDelivery.Pinned), null, "the-app", null);
    [Fact] void should_keep_equal_requests_pending_before_the_leader_is_released() => _equalRequestsPendingBeforeRelease.ShouldBeTrue();
    [Fact] void should_queue_all_distinct_registrations_before_the_leader_is_released() => _registrationsQueuedBeforeRelease.ShouldEqual(4);
    [Fact] void should_queue_changed_content_for_the_same_projection_separately() => _queuedRegistrations.Received(1).Register(Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Single().Identifier == "the-projection" && definitions.Single().ReadModel == "another-read-model"), null, "the-app", null);
    [Fact] void should_queue_a_different_full_set_owner_separately() => _queuedRegistrations.Received(1).Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Client, "the-app", null);
    [Fact] void should_queue_a_different_generation_delivery_separately() => _queuedRegistrations.Received(1).Register(Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Single().GenerationDelivery == EventGenerationDelivery.Pinned), null, "the-app", null);
    [Fact] void should_complete_the_joiners_with_the_leader() => (_joiner.IsCompletedSuccessfully && _anotherJoiner.IsCompletedSuccessfully).ShouldBeTrue();
}
