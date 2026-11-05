// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

/// <summary>
/// Everyone waiting on a registration gets its outcome, a failure included, and a failed registration is not
/// remembered: the retry that follows leads a new one.
/// </summary>
public class and_the_shared_registration_fails : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _firstFails = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _queuedRegistrations;
    Exception _leaderFailure;
    Exception _joinerFailure;

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _queuedRegistrations
            .Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<Concepts.Projections.ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(_ => _firstFails.Task, _ => Task.CompletedTask);
        _silo.AddProbe(_ => _queuedRegistrations);
    }

    async Task Because()
    {
        static ProjectionDefinition[] Definitions() => [CreateDefinition("the-projection", "the-read-model")];

        var leader = _grain.RegisterShared("the-signature", Definitions());
        var joiner = _grain.RegisterShared("the-signature", Definitions());
        _firstFails.SetException(new InvalidOperationException("the registration failed"));
        _leaderFailure = await Catch.Exception(() => leader);
        _joinerFailure = await Catch.Exception(() => joiner);
        await _grain.RegisterShared("the-signature", Definitions());
    }

    [Fact] void should_fail_the_first_caller() => _leaderFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_the_joined_caller_the_same_way() => _joinerFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_queue_the_retry_as_a_registration_of_its_own() => _queuedRegistrations.ReceivedWithAnyArgs(2).Register(default!, default, default, default);
}
