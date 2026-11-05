// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Statistics;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_shared;

public class and_a_storm_of_identical_registrations_arrives : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _queuedRegistrations;
    IProjectionsManager _kernelManager;
    KernelProjections _kernel;
    Task[] _callers;
    int _turnsBeforeRelease;
    bool _allPending;

    void Establish()
    {
        _queuedRegistrations = Substitute.For<IProjectionsManager>();
        _queuedRegistrations.Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(_ => _release.Task);
        _silo.AddProbe(_ => _queuedRegistrations);

        _kernelManager = Substitute.For<IProjectionsManager>();
        _kernelManager.RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(call => _grain.RegisterShared(call.ArgAt<string>(0), call.ArgAt<IEnumerable<ProjectionDefinition>>(1), call.ArgAt<ProjectionOwner?>(2), call.ArgAt<string?>(3), call.ArgAt<string?>(4)));
        _kernelManager.Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), Arg.Any<ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(call => _queuedRegistrations.Register(call.ArgAt<IEnumerable<ProjectionDefinition>>(0), call.ArgAt<ProjectionOwner?>(1), call.ArgAt<string?>(2), call.ArgAt<string?>(3)));
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<IProjectionsManager>(EventStore).Returns(_kernelManager);
        factory.GetGrain<IReadModelsManager>(EventStore).Returns(Substitute.For<IReadModelsManager>());
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(EventTypeStatisticsProjection)]);
        _kernel = new KernelProjections(types, factory, Options.Create(new ChronicleOptions()));
    }

    async Task Because()
    {
        var definitions = Enumerable.Range(0, 50).Select(index => CreateDefinition($"studio-projection-{index}", "studio-read-model")).ToArray();
        Task Register(string version) => _grain.RegisterShared(ProjectionRegistrationSignature.For(definitions, ProjectionOwner.Client, "studio", version), definitions, ProjectionOwner.Client, "studio", version);
        _callers = [.. Enumerable.Range(0, 200).Select(_ => Register("1.0")), _kernel.DiscoverAndRegister(EventStore), Register("2.0")];
        _turnsBeforeRelease = _queuedRegistrations.ReceivedCalls().Count();
        _allPending = _callers.All(task => !task.IsCompleted);
        _release.SetResult();
        await Task.WhenAll(_callers);
    }

    [Fact] void should_queue_one_turn_per_distinct_signature() => _turnsBeforeRelease.ShouldEqual(3);
    [Fact] void should_keep_all_202_callers_pending_until_release() => _allPending.ShouldBeTrue();
    [Fact] void should_complete_every_caller() => _callers.Count(task => task.IsCompletedSuccessfully).ShouldEqual(202);
    [Fact] void should_share_the_kernels_registration() => _kernelManager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null);
    [Fact] void should_not_queue_an_unshared_kernel_turn() => _kernelManager.DidNotReceiveWithAnyArgs().Register(default!);
    [Fact] void should_include_the_kernel_among_the_three_turns() => _queuedRegistrations.Received(1).Register(Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Any() && definitions.All(definition => definition.Owner == ProjectionOwner.Kernel)), ProjectionOwner.Kernel, null, null);
}
