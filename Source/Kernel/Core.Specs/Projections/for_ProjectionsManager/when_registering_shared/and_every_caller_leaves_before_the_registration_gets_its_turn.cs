// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

public class and_every_caller_leaves_before_the_registration_gets_its_turn : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _queuedRegistrations;
    GrainCancellationTokenSource _caller;
    ProjectionDefinition[] _definitions;
    string _signature;
    Task _registration;
    int _namespaceLookupsBeforeTurn;

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _queuedRegistrations.Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(_ => _hold.Task);
        _silo.AddProbe(_ => _queuedRegistrations);
        _caller = new();
        _definitions = [CreateDefinition("projection", "read-model")];
        _signature = ProjectionRegistrationSignature.For(_definitions, ProjectionOwner.Client, "studio", "1.0");
    }

    async Task Because()
    {
        _registration = _grain.RegisterShared(_signature, _definitions, ProjectionOwner.Client, "studio", "1.0", _caller.Token);
        await _caller.Cancel();
        using var timeout = new CancellationTokenSource();
        await Task.WhenAny(_registration, Task.Delay(TimeSpan.FromSeconds(5), TimeProvider.System, timeout.Token));
        await timeout.CancelAsync();
        _namespaceLookupsBeforeTurn = _namespaces.ReceivedCalls().Count();

        // The turn the registration was queued for arrives after its callers have gone.
        await _grain.Register(_definitions, ProjectionOwner.Client, "studio", "1.0");
        _hold.SetResult();
    }

    [Fact] void should_have_asked_the_grain_to_register_once() => _queuedRegistrations.ReceivedWithAnyArgs(1).Register(default!);
    [Fact] void should_cancel_the_caller_that_left() => _registration.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_apply_the_registration_when_its_turn_arrives() => _namespaces.ReceivedCalls().Count().ShouldEqual(_namespaceLookupsBeforeTurn);
}
