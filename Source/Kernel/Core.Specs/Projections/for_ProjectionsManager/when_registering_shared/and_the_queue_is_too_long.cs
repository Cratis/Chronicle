// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Registrations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

public class and_the_queue_is_too_long : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _queuedRegistrations;
    Task _running;
    Exception _failure;
    int _registrationsAskedFor;

    protected override ChronicleOptions CreateOptions() => new() { Registration = new() { MaxConcurrentRegistrations = 1, MaxQueueAgeSeconds = 1, BusyRetryAfterSeconds = 9 } };

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _queuedRegistrations.Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(_ => _hold.Task);
        _silo.AddProbe(_ => _queuedRegistrations);
    }

    async Task Because()
    {
        Task Register(string identifier)
        {
            ProjectionDefinition[] definitions = [CreateDefinition(identifier, "read-model")];
            return _grain.RegisterShared(ProjectionRegistrationSignature.For(definitions, ProjectionOwner.Client, "studio", "1.0"), definitions, ProjectionOwner.Client, "studio", "1.0");
        }

        _running = Register("first");
        var waiting = Register("second");
        _failure = await Catch.Exception(async () => await waiting);
        _registrationsAskedFor = _queuedRegistrations.ReceivedCalls().Count();
        _hold.SetResult();
        await _running;
    }

    [Fact] void should_answer_busy() => _failure.ShouldBeOfExactType<RegistrationBusyException>();
    [Fact] void should_say_when_to_retry() => ((RegistrationBusyException)_failure).RetryAfter.ShouldEqual(TimeSpan.FromSeconds(9));
    [Fact] void should_not_ask_the_grain_to_apply_the_one_that_waited_too_long() => _registrationsAskedFor.ShouldEqual(1);
    [Fact] void should_let_the_running_registration_finish() => _running.IsCompletedSuccessfully.ShouldBeTrue();
}
