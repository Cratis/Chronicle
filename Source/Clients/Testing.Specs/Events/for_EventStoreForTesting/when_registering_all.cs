// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

/// <summary>
/// Exercises the registration outcome through nothing but the public event store surface. The in-process event store
/// has no kernel to register with, so the point being pinned is the transition a consumer depends on: not run until
/// registration has run, then an outcome per declared artifact - the same shape a live event store has.
/// </summary>
public class when_registering_all : Specification
{
    EventStoreForTesting _eventStore;
    IClientArtifactsProvider _clientArtifactsProvider;
    RegistrationOutcome _beforeRegistering;
    RegistrationOutcome _afterRegistering;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _clientArtifactsProvider.EventTypes.Returns([typeof(ModuleCreated)]);
        _clientArtifactsProvider.ModelBoundProjections.Returns([typeof(SimpleModule)]);
        _eventStore = new EventStoreForTesting(null, _clientArtifactsProvider);
    }

    async Task Because()
    {
        _beforeRegistering = _eventStore.Registration;
        await _eventStore.RegisterAll();
        _afterRegistering = await _eventStore.WaitForRegistration();
    }

    [Fact] void should_not_report_registration_as_run_before_it_ran() => _beforeRegistering.HasRun.ShouldBeFalse();
    [Fact] void should_report_no_artifacts_before_registration_ran() => _beforeRegistering.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_registration_as_run_afterwards() => _afterRegistering.HasRun.ShouldBeTrue();
    [Fact] void should_report_the_read_model_it_discovered() => _afterRegistering.Artifacts.Select(_ => _.ArtifactType).ShouldContain(typeof(SimpleModule));
    [Fact] void should_report_that_read_model_as_registered() => _afterRegistering.Artifacts.Single(_ => _.ArtifactType == typeof(SimpleModule)).IsRegistered.ShouldBeTrue();
    [Fact] void should_report_no_failures() => _afterRegistering.Failures.ShouldBeEmpty();
}
