// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// The three states a consumer could not previously tell apart: everything registered, some subset registered and the
/// rest were dropped, and registration has not run yet. Nothing throws in the middle case - the read model is logged and
/// then simply absent - so without an outcome the second and the first look the same from outside.
/// </summary>
public class and_one_projection_could_not_be_built : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _beforeRegistering;
    RegistrationOutcome _afterRegistering;

    async Task Because()
    {
        _beforeRegistering = _eventStore.Registration;
        await _eventStore.RegisterAll();
        _afterRegistering = _eventStore.Registration;
    }

    [Fact] void should_not_report_registration_as_run_before_it_ran() => _beforeRegistering.HasRun.ShouldBeFalse();
    [Fact] void should_report_no_artifacts_before_registration_ran() => _beforeRegistering.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_registration_as_run_afterwards() => _afterRegistering.HasRun.ShouldBeTrue();
    [Fact] void should_report_an_outcome_for_every_declared_projection() => _afterRegistering.Artifacts.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(BuildableProjection), typeof(UnbuildableProjection)]);
    [Fact] void should_report_the_projection_that_could_be_built_as_registered() => _afterRegistering.Artifacts.Single(_ => _.ArtifactType == typeof(BuildableProjection)).IsRegistered.ShouldBeTrue();
    [Fact] void should_report_only_the_projection_that_could_not_be_built_as_failed() => _afterRegistering.Failures.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(UnbuildableProjection)]);
    [Fact] void should_carry_the_failure_that_stopped_it() => _afterRegistering.Failures.Single().Failure.ShouldBeOfExactType<ProjectionCannotBeDefined>();
    [Fact] void should_not_report_the_registration_as_successful() => _afterRegistering.IsSuccess.ShouldBeFalse();
}
