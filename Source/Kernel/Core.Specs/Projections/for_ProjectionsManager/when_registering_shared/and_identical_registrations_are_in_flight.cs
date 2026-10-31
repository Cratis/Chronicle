// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        _leader = _grain.RegisterShared("the-signature", Definitions(), registrant: "the-app");
        _joiner = _grain.RegisterShared("the-signature", Definitions(), registrant: "the-app");
        _anotherJoiner = _grain.RegisterShared("the-signature", Definitions(), registrant: "the-app");
        _different = _grain.RegisterShared("another-signature", Definitions(), registrant: "another-app");
        _firstFinishes.SetResult();
        await Task.WhenAll(_leader, _joiner, _anotherJoiner, _different);
    }

    [Fact] void should_queue_the_work_once_for_identical_registrations() => _queuedRegistrations.Received(1).Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), null, "the-app", null);
    [Fact] void should_queue_a_different_registration_separately() => _queuedRegistrations.Received(1).Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), null, "another-app", null);
    [Fact] void should_complete_the_joiners_with_the_leader() => (_joiner.IsCompletedSuccessfully && _anotherJoiner.IsCompletedSuccessfully).ShouldBeTrue();
}
